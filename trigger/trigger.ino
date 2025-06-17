const int adcPin = 28;  // GPIO 28 = physical pin 34 = ADC2

void setup() {
  //pinMode(LED_BUILTIN, OUTPUT);
  //analogReadResolution(12);  // Optional: set 12-bit resolution (0–4095)
  Serial.begin(115200);
  while (!Serial);  // Wait for USB serial on Pico W
}

unsigned long ms;
int peak = 0;
int mode = 0;

const int ATTACK_THRESHOLD = 40;
const int DECAY_THRESHOLD = 30;
const int RING = 5;
const unsigned long DELAY = 10;
const double MAX = 1023;
const double RANGE = MAX - ATTACK_THRESHOLD;

void loop() {
  int value = analogRead(adcPin);  // Read from ADC2 (GPIO 28)
  //Serial.println(value);

  switch (mode)
  {
    case 0: // waiting for attack & peak
      if (value > ATTACK_THRESHOLD && value > peak)
      {
        peak = value;
        ms = millis();
      }
      if (value < peak - RING)
      {
        byte velocity = ((double)(peak - ATTACK_THRESHOLD) * 126.0 / RANGE) + 1.5; // 1 - 127
        //digitalWrite(LED_BUILTIN, HIGH);
        Serial.write(velocity);
        //Serial.println(peak);
        Serial.flush();
        yield();
        //delay(100);
        //digitalWrite(LED_BUILTIN, LOW);
        peak = 0;
        mode = 1;
      }
      break;
    case 1: // waiting for decay
      unsigned long elapsed = millis() - ms;
      if (elapsed > DELAY && value < max(peak - DECAY_THRESHOLD, DECAY_THRESHOLD))
      {
        //Serial.println("----");
        peak = 0;
        mode = 0;
      }
      break;
  }
}

/*
ADC0	GP26	Pin 31
ADC1	GP27	Pin 32
ADC2	GP28	Pin 34

3	GND
8	GND
13	GND
18	GND
23	GND
28	GND
33	GND
38	GND

[Piezo]----+-----> To ADC (e.g. GP28)
           |
         [1MΩ]
           |
         +----[1N5819]-----> 3.3V  ← clamps at ~3.4V
           |
         [220kΩ]
           |
          GND

https://my.eng.utah.edu/~cs5789/handouts/piezo.pdf
*/