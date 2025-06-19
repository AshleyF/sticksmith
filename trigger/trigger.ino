//#define DEBUG

const int adcPin = 28;  // GPIO 28 = physical pin 34 = ADC2

void setup() {
  Serial.begin(115200);
  while (!Serial);  // Wait for USB serial on Pico W
}

const double GAIN = 5.0;
const int THRESHOLD = 25 * GAIN; // observed 110
const double MAX = 1023;
const double RANGE = MAX - THRESHOLD;
const unsigned long DELAY = 30000; // observed 1.1-1.7ms
const unsigned long MASK = 60000; // observed 500-700ms

int trigger(int value, unsigned long time, unsigned long *hitTime, unsigned long *peakTime, int *peak, int *mode)
{
  if (*mode < 2 && value > *peak)
  {
    *peak = value;
    *peakTime = time;
    if (*mode == 0 /* waiting for hit */)
    {
      *hitTime = time;
      *mode = 1; // delay for peak
    }
  }
  if (*mode > 0)
  {
    unsigned long elapsed = time - *hitTime;
    if (*mode == 1 /* delay for peak*/ && elapsed > DELAY)
    {
#ifdef DEBUG
      Serial.print("hit ");
      Serial.print(*peak);
      Serial.print(" peak: ");
      Serial.print(*peakTime - *hitTime);
#endif
      int hit = *peak;
      *peak = THRESHOLD;
      *mode = 2 /* wait for decay*/;
      return hit;
    }
    if (*mode == 2 && value < THRESHOLD)
    {
#ifdef DEBUG
      Serial.print(" decay: ");
      Serial.println(time - *hitTime);
#endif
      *mode = 3; // wait for mask time
    }
    if (*mode == 3 /* mask for decay*/ && elapsed > MASK)
    {
      *mode = 0; // waiting for hit
    }
  }
  return 0;
}

unsigned long hitTime0;
unsigned long peakTime0;

int peak0 = THRESHOLD;
int mode0 = 0; // waiting for hit

void loop() {
  int value = analogRead(adcPin) * GAIN;  // Read from ADC2 (GPIO 28)
  //Serial.println(value);
  unsigned long time = micros();
  int hit = trigger(value, time, &hitTime0, &peakTime0, &peak0, &mode0);
  if (hit > 0)
  {
    byte velocity = ((double)(hit - THRESHOLD) * 255 / RANGE) + 0.5; // 1 - 127
    Serial.write(velocity);
    Serial.flush();
    yield();
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