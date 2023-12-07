/*
The code snippet (1. Make the Circuit Playground Express as the mouse to control unity ) below has been sourced from
Circuit Playground Express Examples of accel_mouse
I have changed ease some functions and add color change when user turn left,right, ,up and down the leds lights will turn on 
adjusted the loop functionality
*/
#include <Adafruit_CircuitPlayground.h>
#include <Mouse.h>

#define SENSITIVITY 10  // Adjust this value to control sensitivity.
#define LED_COUNT 10   // Number of NeoPixel LEDs on the Circuit Playground.

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

  if (x > 2) {  // Tilted to the right
    // Turn on the right half of the NeoPixels (LEDs 5 to 9).
    for (int i = 0; i < LED_COUNT / 2; i++) {
      CircuitPlayground.setPixelColor(i, 255, 0, 0); // Red
    }
    for (int i = LED_COUNT / 2; i < LED_COUNT; i++) {
      CircuitPlayground.setPixelColor(i, 0, 0, 0); // Turn off the left half
    }
  } else if (x < -2) {  // Tilted to the left
    // Turn on the left half of the NeoPixels (LEDs 0 to 4).
    for (int i = 0; i < LED_COUNT / 2; i++) {
      CircuitPlayground.setPixelColor(i, 0, 0, 0); // Turn off the right half
    }
    for (int i = LED_COUNT / 2; i < LED_COUNT; i++) {
      CircuitPlayground.setPixelColor(i, 0, 0, 255); // Blue
    }
    }else if (y < -2) {  // Tilted to the up
    // Turn on the all of the NeoPixels 
    for (int i = 0; i < LED_COUNT % 2; i++) {
      CircuitPlayground.setPixelColor(i, 0, 0, 0); // Turn on the all leds
    }
    for (int i = LED_COUNT % 2; i < LED_COUNT; i++) {
      CircuitPlayground.setPixelColor(i, 255, 255, 0); // Yellow
    }
  } 
  else if (y > 2) {  // Tilted to the down
    // Turn on all of the NeoPixels 
    for (int i = 0; i < LED_COUNT % 2; i++) {
      CircuitPlayground.setPixelColor(i, 0, 0, 0); // Turn on the all leds
    }
    for (int i = LED_COUNT % 2; i < LED_COUNT; i++) {
      CircuitPlayground.setPixelColor(i, 128, 128, 128); // white
    }
    }else {
    CircuitPlayground.clearPixels(); // Turn off all NeoPixels if not tilted left or right or up or down
  }

  if (CircuitPlayground.leftButton()) {
    Mouse.press(MOUSE_LEFT);
  } else {
    Mouse.release(MOUSE_LEFT);
  }

  if (CircuitPlayground.rightButton()) {
    Mouse.press(MOUSE_RIGHT);
  } else {
    Mouse.release(MOUSE_RIGHT);
  }

  delay(10);
}
// End code snippet (1. Make the Circuit Playground Express as the mouse to control unity )
