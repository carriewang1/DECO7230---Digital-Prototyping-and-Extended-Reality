/*
The code snippet (2. Make the Circuit Playground Express as the mouse to control unity with single led) below has been sourced from
Circuit Playground Express Examples of accel_mouse
I have changed ease some functions and add color change when user turn left,right,up and down the leds lights will turn on and 
adjusted the loop functionality
*/
#include <Adafruit_CircuitPlayground.h>
#include <Mouse.h>

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
  if (!CircuitPlayground.slideSwitch()) {
    return;
  }

  float x = CircuitPlayground.motionX();
  float y = CircuitPlayground.motionY();

  int moveX = lerp(x, -10, 10, -SENSITIVITY, SENSITIVITY);
  int moveY = lerp(y, -10, 10, -SENSITIVITY, SENSITIVITY);

  Mouse.move(moveX, moveY);

  if (x > 2) {
    CircuitPlayground.setPixelColor(0, 255, 0, 0); //  when user turn right the led with turn Red
  } else if (x < -2) {
    CircuitPlayground.setPixelColor(0, 0, 0, 255);// // when user turn left the led with turn Blue
  } else if (y > 2) {
    CircuitPlayground.setPixelColor(0, 0, 255, 0); // when user turn down the led with turn Green
  } else if (y < -2) {
    CircuitPlayground.setPixelColor(0, 255, 255, 0); // when user turn up the led with turn Yellow
  } else {
    CircuitPlayground.clearPixels(); // Turn off NeoPixel if not tilted left or right or up or down
  }

  if (CircuitPlayground.leftButton()) {
    Mouse.press(MOUSE_LEFT); // mouse left click hit the ball
  } else {
    Mouse.release(MOUSE_LEFT);
  }

  if (CircuitPlayground.rightButton()) {
    Mouse.press(MOUSE_RIGHT);
  } else {
    Mouse.release(MOUSE_RIGHT);
  }

  delay(10);
  // Delay for stability and to avoid rapid button presses.
}
// End code snippet (2. Make the Circuit Playground Express as the mouse to control unity with single led)

