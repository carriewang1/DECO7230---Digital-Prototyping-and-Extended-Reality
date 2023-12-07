
/*
The code snippet RBFPScONTROLLER below has been sourced from
[https://assetstore.unity.com/packages/tools/physics/trajectory-predictor-55752
The code snippet appears in its original form
*/


using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrajectoryExample {
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class RBFPSController : MonoBehaviour {
        [Serializable]
        public class MovementSettings {
            public float ForwardSpeed = 8.0f;   // Speed when walking forward
            public float BackwardSpeed = 4.0f;  // Speed when walking backwards
            public float StrafeSpeed = 4.0f;    // Speed when walking sideways
            public float RunMultiplier = 2.0f;   // Speed when sprinting
            public KeyCode RunKey = KeyCode.LeftShift;
            public float JumpForce = 30f;
            public AnimationCurve SlopeCurveModifier = new AnimationCurve(new Keyframe(-90.0f, 1.0f), new Keyframe(0.0f, 1.0f), new Keyframe(90.0f, 0.0f));
            [HideInInspector] public float CurrentTargetSpeed = 8f;

#if !MOBILE_INPUT
            private bool m_Running;
#endif

            public void UpdateDesiredTargetSpeed(Vector2 input) {
                if (input == Vector2.zero) return;
                if (input.x > 0 || input.x < 0) {
                    //strafe
                    CurrentTargetSpeed = StrafeSpeed;
                }
                if (input.y < 0) {
                    //backwards
                    CurrentTargetSpeed = BackwardSpeed;
                }
                if (input.y > 0) {
                    //forwards
                    //handled last as if strafing and moving forward at the same time forwards speed should take precedence
                    CurrentTargetSpeed = ForwardSpeed;
                }
#if !MOBILE_INPUT
                if (Input.GetKey(RunKey)) {
                    CurrentTargetSpeed *= RunMultiplier;
                    m_Running = true;
                } else {
                    m_Running = false;
                }
#endif
            }

#if !MOBILE_INPUT
            public bool Running {
                get { return m_Running; }
            }
#endif
        }


        [Serializable]
        public class AdvancedSettings {
            public float groundCheckDistance = 0.01f; // distance for checking if the controller is grounded ( 0.01f seems to work best for this )
            public float stickToGroundHelperDistance = 0.5f; // stops the character
            public float slowDownRate = 20f; // rate at which the controller comes to a stop when there is no input
            public bool airControl; // can the user control the direction that is being moved in the air
            [Tooltip("set it to 0.1 or more if you get stuck in wall")]
            public float shellOffset; //reduce the radius by that ratio to avoid getting stuck in wall (a value of 0.1f is nice)
        }


        public Camera cam;
        public MovementSettings movementSettings = new MovementSettings();
        public MouseLook mouseLook = new MouseLook();
        public AdvancedSettings advancedSettings = new AdvancedSettings();


        private Rigidbody m_RigidBody;
        private CapsuleCollider m_Capsule;
        private float m_YRotation;
        private Vector3 m_GroundContactNormal;
        private bool m_Jump, m_PreviouslyGrounded, m_Jumping, m_IsGrounded;


        public Vector3 Velocity {
            get { return m_RigidBody.velocity; }
        }

        public bool Grounded {
            get { return m_IsGrounded; }
        }

        public bool Jumping {
            get { return m_Jumping; }
        }

        public bool Running {
            get {
#if !MOBILE_INPUT
                return movementSettings.Running;
#else
	            return false;
#endif
            }
        }


        private void Start() {
            m_RigidBody = GetComponent<Rigidbody>();
            m_Capsule = GetComponent<CapsuleCollider>();
            mouseLook.Init(transform, cam.transform);
        }


        private void Update() {
            RotateView();

            if (CrossPlatformInputManager.GetButtonDown("Jump") && !m_Jump) {
                m_Jump = true;
            }
        }


        private void FixedUpdate() {
            GroundCheck();
            Vector2 input = GetInput();

            if ((Mathf.Abs(input.x) > float.Epsilon || Mathf.Abs(input.y) > float.Epsilon) && (advancedSettings.airControl || m_IsGrounded)) {
                // always move along the camera forward as it is the direction that it being aimed at
                Vector3 desiredMove = cam.transform.forward * input.y + cam.transform.right * input.x;
                desiredMove = Vector3.ProjectOnPlane(desiredMove, m_GroundContactNormal).normalized;

                desiredMove.x = desiredMove.x * movementSettings.CurrentTargetSpeed;
                desiredMove.z = desiredMove.z * movementSettings.CurrentTargetSpeed;
                desiredMove.y = desiredMove.y * movementSettings.CurrentTargetSpeed;
                if (m_RigidBody.velocity.sqrMagnitude <
                    (movementSettings.CurrentTargetSpeed * movementSettings.CurrentTargetSpeed)) {
                    m_RigidBody.AddForce(desiredMove * SlopeMultiplier(), ForceMode.Impulse);
                }
            }

            if (m_IsGrounded) {
                m_RigidBody.drag = 5f;

                if (m_Jump) {
                    m_RigidBody.drag = 0f;
                    m_RigidBody.velocity = new Vector3(m_RigidBody.velocity.x, 0f, m_RigidBody.velocity.z);
                    m_RigidBody.AddForce(new Vector3(0f, movementSettings.JumpForce, 0f), ForceMode.Impulse);
                    m_Jumping = true;
                }

                if (!m_Jumping && Mathf.Abs(input.x) < float.Epsilon && Mathf.Abs(input.y) < float.Epsilon && m_RigidBody.velocity.magnitude < 1f) {
                    m_RigidBody.Sleep();
                }
            } else {
                m_RigidBody.drag = 0f;
                if (m_PreviouslyGrounded && !m_Jumping) {
                    StickToGroundHelper();
                }
            }
            m_Jump = false;
        }


        private float SlopeMultiplier() {
            float angle = Vector3.Angle(m_GroundContactNormal, Vector3.up);
            return movementSettings.SlopeCurveModifier.Evaluate(angle);
        }


        private void StickToGroundHelper() {
            RaycastHit hitInfo;
            if (Physics.SphereCast(transform.position, m_Capsule.radius * (1.0f - advancedSettings.shellOffset), Vector3.down, out hitInfo,
                                   ((m_Capsule.height / 2f) - m_Capsule.radius) +
                                   advancedSettings.stickToGroundHelperDistance, ~0, QueryTriggerInteraction.Ignore)) {
                if (Mathf.Abs(Vector3.Angle(hitInfo.normal, Vector3.up)) < 85f) {
                    m_RigidBody.velocity = Vector3.ProjectOnPlane(m_RigidBody.velocity, hitInfo.normal);
                }
            }
        }


        private Vector2 GetInput() {

            Vector2 input = new Vector2 {
                x = CrossPlatformInputManager.GetAxis("Horizontal"),
                y = CrossPlatformInputManager.GetAxis("Vertical")
            };
            movementSettings.UpdateDesiredTargetSpeed(input);
            return input;
        }


        private void RotateView() {
            //avoids the mouse looking if the game is effectively paused
            if (Mathf.Abs(Time.timeScale) < float.Epsilon) return;

            // get the rotation before it's changed
            float oldYRotation = transform.eulerAngles.y;

            mouseLook.LookRotation(transform, cam.transform);

            if (m_IsGrounded || advancedSettings.airControl) {
                // Rotate the rigidbody velocity to match the new direction that the character is looking
                Quaternion velRotation = Quaternion.AngleAxis(transform.eulerAngles.y - oldYRotation, Vector3.up);
                m_RigidBody.velocity = velRotation * m_RigidBody.velocity;
            }
        }

        /// sphere cast down just beyond the bottom of the capsule to see if the capsule is colliding round the bottom
        private void GroundCheck() {
            m_PreviouslyGrounded = m_IsGrounded;
            RaycastHit hitInfo;
            if (Physics.SphereCast(transform.position, m_Capsule.radius * (1.0f - advancedSettings.shellOffset), Vector3.down, out hitInfo,
                                   ((m_Capsule.height / 2f) - m_Capsule.radius) + advancedSettings.groundCheckDistance, ~0, QueryTriggerInteraction.Ignore)) {
                m_IsGrounded = true;
                m_GroundContactNormal = hitInfo.normal;
            } else {
                m_IsGrounded = false;
                m_GroundContactNormal = Vector3.up;
            }
            if (!m_PreviouslyGrounded && m_IsGrounded && m_Jumping) {
                m_Jumping = false;
            }
        }
    }



    [Serializable]
    public class MouseLook {
        public float XSensitivity = 2f;
        public float YSensitivity = 2f;
        public bool clampVerticalRotation = true;
        public float MinimumX = -90F;
        public float MaximumX = 90F;
        public bool smooth;
        public float smoothTime = 5f;
        public bool lockCursor = true;


        private Quaternion m_CharacterTargetRot;
        private Quaternion m_CameraTargetRot;
        private bool m_cursorIsLocked = true;

        public void Init(Transform character, Transform camera) {
            m_CharacterTargetRot = character.localRotation;
            m_CameraTargetRot = camera.localRotation;
        }


        public void LookRotation(Transform character, Transform camera) {
            float yRot = CrossPlatformInputManager.GetAxis("Mouse X") * XSensitivity;
            float xRot = CrossPlatformInputManager.GetAxis("Mouse Y") * YSensitivity;

            m_CharacterTargetRot *= Quaternion.Euler(0f, yRot, 0f);
            m_CameraTargetRot *= Quaternion.Euler(-xRot, 0f, 0f);

            if (clampVerticalRotation)
                m_CameraTargetRot = ClampRotationAroundXAxis(m_CameraTargetRot);

            if (smooth) {
                character.localRotation = Quaternion.Slerp(character.localRotation, m_CharacterTargetRot,
                    smoothTime * Time.deltaTime);
                camera.localRotation = Quaternion.Slerp(camera.localRotation, m_CameraTargetRot,
                    smoothTime * Time.deltaTime);
            } else {
                character.localRotation = m_CharacterTargetRot;
                camera.localRotation = m_CameraTargetRot;
            }

            UpdateCursorLock();
        }

        public void SetCursorLock(bool value) {
            lockCursor = value;
            if (!lockCursor) {//we force unlock the cursor if the user disable the cursor locking helper
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public void UpdateCursorLock() {
            //if the user set "lockCursor" we check & properly lock the cursos
            if (lockCursor)
                InternalLockUpdate();
        }

        private void InternalLockUpdate() {
            if (Input.GetKeyUp(KeyCode.Escape)) {
                m_cursorIsLocked = false;
            } else if (Input.GetMouseButtonUp(0)) {
                m_cursorIsLocked = true;
            }

            if (m_cursorIsLocked) {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            } else if (!m_cursorIsLocked) {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        Quaternion ClampRotationAroundXAxis(Quaternion q) {
            q.x /= q.w;
            q.y /= q.w;
            q.z /= q.w;
            q.w = 1.0f;

            float angleX = 2.0f * Mathf.Rad2Deg * Mathf.Atan(q.x);

            angleX = Mathf.Clamp(angleX, MinimumX, MaximumX);

            q.x = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleX);

            return q;
        }

    }



    #region Cross Platform Garbo

    public static class CrossPlatformInputManager {
        public enum ActiveInputMethod {
            Hardware,
            Touch
        }


        private static VirtualInput activeInput;

        private static VirtualInput s_TouchInput;
        private static VirtualInput s_HardwareInput;


        static CrossPlatformInputManager() {
            s_TouchInput = new MobileInput();
            s_HardwareInput = new StandaloneInput();
#if MOBILE_INPUT
            activeInput = s_TouchInput;
#else
            activeInput = s_HardwareInput;
#endif
        }

        public static void SwitchActiveInputMethod(ActiveInputMethod activeInputMethod) {
            switch (activeInputMethod) {
                case ActiveInputMethod.Hardware:
                    activeInput = s_HardwareInput;
                    break;

                case ActiveInputMethod.Touch:
                    activeInput = s_TouchInput;
                    break;
            }
        }

        public static bool AxisExists(string name) {
            return activeInput.AxisExists(name);
        }

        public static bool ButtonExists(string name) {
            return activeInput.ButtonExists(name);
        }

        public static void RegisterVirtualAxis(VirtualAxis axis) {
            activeInput.RegisterVirtualAxis(axis);
        }


        public static void RegisterVirtualButton(VirtualButton button) {
            activeInput.RegisterVirtualButton(button);
        }


        public static void UnRegisterVirtualAxis(string name) {
            if (name == null) {
                throw new ArgumentNullException("name");
            }
            activeInput.UnRegisterVirtualAxis(name);
        }


        public static void UnRegisterVirtualButton(string name) {
            activeInput.UnRegisterVirtualButton(name);
        }


        // returns a reference to a named virtual axis if it exists otherwise null
        public static VirtualAxis VirtualAxisReference(string name) {
            return activeInput.VirtualAxisReference(name);
        }


        // returns the platform appropriate axis for the given name
        public static float GetAxis(string name) {
            return GetAxis(name, false);
        }


        public static float GetAxisRaw(string name) {
            return GetAxis(name, true);
        }


        // private function handles both types of axis (raw and not raw)
        private static float GetAxis(string name, bool raw) {
            return activeInput.GetAxis(name, raw);
        }


        // -- Button handling --
        public static bool GetButton(string name) {
            return activeInput.GetButton(name);
        }


        public static bool GetButtonDown(string name) {
            return activeInput.GetButtonDown(name);
        }


        public static bool GetButtonUp(string name) {
            return activeInput.GetButtonUp(name);
        }


        public static void SetButtonDown(string name) {
            activeInput.SetButtonDown(name);
        }


        public static void SetButtonUp(string name) {
            activeInput.SetButtonUp(name);
        }


        public static void SetAxisPositive(string name) {
            activeInput.SetAxisPositive(name);
        }


        public static void SetAxisNegative(string name) {
            activeInput.SetAxisNegative(name);
        }


        public static void SetAxisZero(string name) {
            activeInput.SetAxisZero(name);
        }


        public static void SetAxis(string name, float value) {
            activeInput.SetAxis(name, value);
        }


        public static Vector3 mousePosition {
            get { return activeInput.MousePosition(); }
        }


        public static void SetVirtualMousePositionX(float f) {
            activeInput.SetVirtualMousePositionX(f);
        }


        public static void SetVirtualMousePositionY(float f) {
            activeInput.SetVirtualMousePositionY(f);
        }


        public static void SetVirtualMousePositionZ(float f) {
            activeInput.SetVirtualMousePositionZ(f);
        }


        // virtual axis and button classes - applies to mobile input
        // Can be mapped to touch joysticks, tilt, gyro, etc, depending on desired implementation.
        // Could also be implemented by other input devices - kinect, electronic sensors, etc
        public class VirtualAxis {
            public string name { get; private set; }
            private float m_Value;
            public bool matchWithInputManager { get; private set; }


            public VirtualAxis(string name)
                : this(name, true) {
            }


            public VirtualAxis(string name, bool matchToInputSettings) {
                this.name = name;
                matchWithInputManager = matchToInputSettings;
            }


            // removes an axes from the cross platform input system
            public void Remove() {
                UnRegisterVirtualAxis(name);
            }


            // a controller gameobject (eg. a virtual thumbstick) should update this class
            public void Update(float value) {
                m_Value = value;
            }


            public float GetValue {
                get { return m_Value; }
            }


            public float GetValueRaw {
                get { return m_Value; }
            }
        }

        // a controller gameobject (eg. a virtual GUI button) should call the
        // 'pressed' function of this class. Other objects can then read the
        // Get/Down/Up state of this button.
        public class VirtualButton {
            public string name { get; private set; }
            public bool matchWithInputManager { get; private set; }

            private int m_LastPressedFrame = -5;
            private int m_ReleasedFrame = -5;
            private bool m_Pressed;


            public VirtualButton(string name)
                : this(name, true) {
            }


            public VirtualButton(string name, bool matchToInputSettings) {
                this.name = name;
                matchWithInputManager = matchToInputSettings;
            }


            // A controller gameobject should call this function when the button is pressed down
            public void Pressed() {
                if (m_Pressed) {
                    return;
                }
                m_Pressed = true;
                m_LastPressedFrame = Time.frameCount;
            }


            // A controller gameobject should call this function when the button is released
            public void Released() {
                m_Pressed = false;
                m_ReleasedFrame = Time.frameCount;
            }


            // the controller gameobject should call Remove when the button is destroyed or disabled
            public void Remove() {
                UnRegisterVirtualButton(name);
            }


            // these are the states of the button which can be read via the cross platform input system
            public bool GetButton {
                get { return m_Pressed; }
            }


            public bool GetButtonDown {
                get {
                    return m_LastPressedFrame - Time.frameCount == -1;
                }
            }


            public bool GetButtonUp {
                get {
                    return (m_ReleasedFrame == Time.frameCount - 1);
                }
            }
        }
    }






    

    public abstract class VirtualInput {
        public Vector3 virtualMousePosition { get; private set; }


        protected Dictionary<string, CrossPlatformInputManager.VirtualAxis> m_VirtualAxes =
            new Dictionary<string, CrossPlatformInputManager.VirtualAxis>();
        // Dictionary to store the name relating to the virtual axes
        protected Dictionary<string, CrossPlatformInputManager.VirtualButton> m_VirtualButtons =
            new Dictionary<string, CrossPlatformInputManager.VirtualButton>();
        protected List<string> m_AlwaysUseVirtual = new List<string>();
        // list of the axis and button names that have been flagged to always use a virtual axis or button


        public bool AxisExists(string name) {
            return m_VirtualAxes.ContainsKey(name);
        }

        public bool ButtonExists(string name) {
            return m_VirtualButtons.ContainsKey(name);
        }


        public void RegisterVirtualAxis(CrossPlatformInputManager.VirtualAxis axis) {
            // check if we already have an axis with that name and log and error if we do
            if (m_VirtualAxes.ContainsKey(axis.name)) {
                Debug.LogError("There is already a virtual axis named " + axis.name + " registered.");
            } else {
                // add any new axes
                m_VirtualAxes.Add(axis.name, axis);

                // if we dont want to match with the input manager setting then revert to always using virtual
                if (!axis.matchWithInputManager) {
                    m_AlwaysUseVirtual.Add(axis.name);
                }
            }
        }


        public void RegisterVirtualButton(CrossPlatformInputManager.VirtualButton button) {
            // check if already have a buttin with that name and log an error if we do
            if (m_VirtualButtons.ContainsKey(button.name)) {
                Debug.LogError("There is already a virtual button named " + button.name + " registered.");
            } else {
                // add any new buttons
                m_VirtualButtons.Add(button.name, button);

                // if we dont want to match to the input manager then always use a virtual axis
                if (!button.matchWithInputManager) {
                    m_AlwaysUseVirtual.Add(button.name);
                }
            }
        }


        public void UnRegisterVirtualAxis(string name) {
            // if we have an axis with that name then remove it from our dictionary of registered axes
            if (m_VirtualAxes.ContainsKey(name)) {
                m_VirtualAxes.Remove(name);
            }
        }


        public void UnRegisterVirtualButton(string name) {
            // if we have a button with this name then remove it from our dictionary of registered buttons
            if (m_VirtualButtons.ContainsKey(name)) {
                m_VirtualButtons.Remove(name);
            }
        }


        // returns a reference to a named virtual axis if it exists otherwise null
        public CrossPlatformInputManager.VirtualAxis VirtualAxisReference(string name) {
            return m_VirtualAxes.ContainsKey(name) ? m_VirtualAxes[name] : null;
        }


        public void SetVirtualMousePositionX(float f) {
            virtualMousePosition = new Vector3(f, virtualMousePosition.y, virtualMousePosition.z);
        }


        public void SetVirtualMousePositionY(float f) {
            virtualMousePosition = new Vector3(virtualMousePosition.x, f, virtualMousePosition.z);
        }


        public void SetVirtualMousePositionZ(float f) {
            virtualMousePosition = new Vector3(virtualMousePosition.x, virtualMousePosition.y, f);
        }


        public abstract float GetAxis(string name, bool raw);

        public abstract bool GetButton(string name);
        public abstract bool GetButtonDown(string name);
        public abstract bool GetButtonUp(string name);

        public abstract void SetButtonDown(string name);
        public abstract void SetButtonUp(string name);
        public abstract void SetAxisPositive(string name);
        public abstract void SetAxisNegative(string name);
        public abstract void SetAxisZero(string name);
        public abstract void SetAxis(string name, float value);
        public abstract Vector3 MousePosition();
    }


    public class StandaloneInput : VirtualInput {
        public override float GetAxis(string name, bool raw) {
            return raw ? Input.GetAxisRaw(name) : Input.GetAxis(name);
        }


        public override bool GetButton(string name) {
            return Input.GetButton(name);
        }


        public override bool GetButtonDown(string name) {
            return Input.GetButtonDown(name);
        }


        public override bool GetButtonUp(string name) {
            return Input.GetButtonUp(name);
        }


        public override void SetButtonDown(string name) {
            throw new Exception(
                " This is not possible to be called for standalone input. Please check your platform and code where this is called");
        }


        public override void SetButtonUp(string name) {
            throw new Exception(
                " This is not possible to be called for standalone input. Please check your platform and code where this is called");
        }


        public override void SetAxisPositive(string name) {
            throw new Exception(
                " This is not possible to be called for standalone input. Please check your platform and code where this is called");
        }


        public override void SetAxisNegative(string name) {
            throw new Exception(
                " This is not possible to be called for standalone input. Please check your platform and code where this is called");
        }


        public override void SetAxisZero(string name) {
            throw new Exception(
                " This is not possible to be called for standalone input. Please check your platform and code where this is called");
        }


        public override void SetAxis(string name, float value) {
            throw new Exception(
                " This is not possible to be called for standalone input. Please check your platform and code where this is called");
        }


        public override Vector3 MousePosition() {
            return Input.mousePosition;
        }
    }

    public class MobileInput : VirtualInput {
        private void AddButton(string name) {
            // we have not registered this button yet so add it, happens in the constructor
            CrossPlatformInputManager.RegisterVirtualButton(new CrossPlatformInputManager.VirtualButton(name));
        }


        private void AddAxes(string name) {
            // we have not registered this button yet so add it, happens in the constructor
            CrossPlatformInputManager.RegisterVirtualAxis(new CrossPlatformInputManager.VirtualAxis(name));
        }


        public override float GetAxis(string name, bool raw) {
            if (!m_VirtualAxes.ContainsKey(name)) {
                AddAxes(name);
            }
            return m_VirtualAxes[name].GetValue;
        }


        public override void SetButtonDown(string name) {
            if (!m_VirtualButtons.ContainsKey(name)) {
                AddButton(name);
            }
            m_VirtualButtons[name].Pressed();
        }


        public override void SetButtonUp(string name) {
            if (!m_VirtualButtons.ContainsKey(name)) {
                AddButton(name);
            }
            m_VirtualButtons[name].Released();
        }


        public override void SetAxisPositive(string name) {
            if (!m_VirtualAxes.ContainsKey(name)) {
                AddAxes(name);
            }
            m_VirtualAxes[name].Update(1f);
        }


        public override void SetAxisNegative(string name) {
            if (!m_VirtualAxes.ContainsKey(name)) {
                AddAxes(name);
            }
            m_VirtualAxes[name].Update(-1f);
        }


        public override void SetAxisZero(string name) {
            if (!m_VirtualAxes.ContainsKey(name)) {
                AddAxes(name);
            }
            m_VirtualAxes[name].Update(0f);
        }


        public override void SetAxis(string name, float value) {
            if (!m_VirtualAxes.ContainsKey(name)) {
                AddAxes(name);
            }
            m_VirtualAxes[name].Update(value);
        }


        public override bool GetButtonDown(string name) {
            if (m_VirtualButtons.ContainsKey(name)) {
                return m_VirtualButtons[name].GetButtonDown;
            }

            AddButton(name);
            return m_VirtualButtons[name].GetButtonDown;
        }


        public override bool GetButtonUp(string name) {
            if (m_VirtualButtons.ContainsKey(name)) {
                return m_VirtualButtons[name].GetButtonUp;
            }

            AddButton(name);
            return m_VirtualButtons[name].GetButtonUp;
        }


        public override bool GetButton(string name) {
            if (m_VirtualButtons.ContainsKey(name)) {
                return m_VirtualButtons[name].GetButton;
            }

            AddButton(name);
            return m_VirtualButtons[name].GetButton;
        }


        public override Vector3 MousePosition() {
            return virtualMousePosition;
        }
    }
#endregion 




}
// End code snippet RBFPScONTROLLER