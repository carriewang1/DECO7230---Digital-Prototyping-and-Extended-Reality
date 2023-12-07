/*
The code snippet (3. Make the Circuit Playground Express as the mouse to control unity with single led) below has been sourced from
Circuit Playground Express Examples of accel_mouse
I have changed ease some functions and add color change when user turn left and right leds lights will turn on and 
adjusted the loop functionality
*/
#include <Adafruit_CircuitPlayground.h>
#include <Mouse.h>

// Configuration values to adjust the sensitivity and speed of the mouse.
#define SENSITIVITY 10  // Adjust this value to control sensitivity.

void setup() {
  CircuitPlayground.begin();
  Mouse.begin();
}
// Floating point linear interpolation function that takes a value inside one
// range and maps it to a new value inside another range.  This is used to transform
// each axis of acceleration to mouse velocity/speed. See this page for details
// on the equation: https://en.wikipedia.org/wiki/Linear_interpolation

float lerp(float value, float start1, float stop1, float start2, float stop2) {
    return (value - start1) / (stop1 - start1) * (stop2 - start2) + start2;
}

void loop() {
  // Check if the slide switch is enabled (on +) and if not, exit the loop.
  if (!CircuitPlayground.slideSwitch()) {
    return;
  }

  // Read accelerometer values.
  float x = CircuitPlayground.motionX();
  float y = CircuitPlayground.motionY();

  // lerp accelerometer values to mouse movement.
  int moveX = lerp(x, -10, 10, -SENSITIVITY, SENSITIVITY);
  int moveY = lerp(y, -10, 10, -SENSITIVITY, SENSITIVITY);

  // Move the mouse.
  Mouse.move(moveX, moveY);

  // Change color based on the tilt direction.
  if (x > 2) {  // Tilted to the right
    CircuitPlayground.setPixelColor(0, 255, 0, 0); // when user titled to the the right , the Led will turn Red
  } else if (x < -2) {  // Tilted to the left
    CircuitPlayground.setPixelColor(0, 0, 0, 255); // when user titled to the the left , the Led will turn Blue
  } 
 
  else {
    CircuitPlayground.clearPixels(); // Turn off NeoPixel if not tilted left or right
  }

  // Check button presses.
  if (CircuitPlayground.leftButton()) { // mouse click to hit the ball
    Mouse.press(MOUSE_LEFT);
  } else {
    Mouse.release(MOUSE_LEFT);
  }

  if (CircuitPlayground.rightButton()) {
    Mouse.press(MOUSE_RIGHT);
  } else {
    Mouse.release(MOUSE_RIGHT);
  }

  delay(10); // Delay for stability and to avoid rapid button presses.
}
// End code snippet (3. Make the Circuit Playground Express as the mouse to control unity with single led)