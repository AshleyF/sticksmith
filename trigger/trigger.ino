//#define DEBUG
#define PULLDATA

const int adcPinA = 26; // GPIO 26 = physical pin 31 = ADC0
const int adcPinB = 27; // GPIO 27 = physical pin 32 = ADC1
const int adcPinC = 28; // GPIO 28 = physical pin 34 = ADC2

void setup() {
  Serial.begin(115200);
  while (!Serial);  // Wait for USB serial on Pico W
}

const double GAIN = 4.0;
const int THRESHOLD = 50;
const double MAX = 1023;
const double RANGE = MAX - THRESHOLD;
const unsigned long DELAY = 2000; // observed 600-750
const unsigned long MASK = 1000000; // observed ~800

int trigger(int value, unsigned long time, unsigned long *hitTime, unsigned long *peakTime, int *peak, int *state)
{
  int rtn = 0;
  //Serial.print(value); Serial.print(' ');
  if (*state < 2 && value > *peak)
  {
    *peak = value;
    *peakTime = time;
    if (*state == 0 /* waiting for hit */)
    {
      *hitTime = time;
      *state = 1; // delay for peak
#ifdef DEBUG
      Serial.println(" state=1");
      Serial.flush();
#endif
    }
  }
  if (*state > 0)
  {
    unsigned long elapsed = time - *hitTime;
    if (*state == 1 /* delay for peak*/ && elapsed > DELAY)
    {
#ifdef DEBUG
      Serial.print("hit ");
      Serial.print(*peak);
      Serial.print(" peak: ");
      Serial.print(*peakTime - *hitTime);
      Serial.flush();
#endif
      int hit = *peak;
      *peak = THRESHOLD;
      *state = 2 /* wait for decay*/;
#ifdef DEBUG
      Serial.println(" state=2");
      Serial.flush();
#endif
      rtn = hit;
      //return hit;
    }
#ifdef DEBUG
    if (*state == 2)
    {
      Serial.print(value);
      Serial.print(' ');
      Serial.print('.');
      Serial.flush();
      delay(100);
    }
#endif
    if (*state == 2 && value < THRESHOLD)
    {
#ifdef DEBUG
      Serial.print(" decay: ");
      Serial.println(time - *hitTime);
      Serial.flush();
#endif
      *state = 3; // wait for mask time
#ifdef DEBUG
      Serial.println(" state=3");
      Serial.flush();
#endif

    }
    if (*state == 3 /* mask for decay*/ && elapsed > MASK)
    {
      *state = 4; // idle
#ifdef DEBUG
      Serial.println(" state=0");
      Serial.flush();
#endif
    }
  }
  return rtn;
}

unsigned long hitTimeA = 0;
unsigned long hitTimeB = 0;
unsigned long hitTimeC = 0;
unsigned long peakTimeA;
unsigned long peakTimeB;
unsigned long peakTimeC;

int peakA = THRESHOLD;
int peakB = THRESHOLD;
int peakC = THRESHOLD;
int stateA = 0; // waiting for hit
int stateB = 0; // waiting for hit
int stateC = 0; // waiting for hit

int lastHitA = 0;
int lastHitB = 0;
int lastHitC = 0;

int loopMode = 0; // waiting for hit

double score(int refA, int refB, int refC, int timeA, int timeB, int timeC)
{
  double a = (refA - timeA);
  double b = (refB - timeB);
  double c = (refC - timeC);
  return sqrt(a * a + b * b + c * c);
}

#ifdef PULLDATA
void loop() {
  int hitA = analogRead(adcPinA);
  int hitB = analogRead(adcPinB);
  int hitC = analogRead(adcPinC);
  Serial.write(255);
  Serial.write((byte)min(254, hitA));
  Serial.write((byte)min(254, hitB));
  Serial.write((byte)min(254, hitC));
  Serial.flush();
  yield();
  delay(1);
}
#else
void loop() {
  unsigned long now = micros();
  int hitA = trigger(analogRead(adcPinA), now, &hitTimeA, &peakTimeA, &peakA, &stateA);
  int hitB = trigger(analogRead(adcPinB), now, &hitTimeB, &peakTimeB, &peakB, &stateB);
  int hitC = trigger(analogRead(adcPinC), now, &hitTimeC, &peakTimeC, &peakC, &stateC);
  delay(100);
  if (loopMode == 0) // waiting for hit
  {
    if (hitA > 0) { lastHitA = hitA; }
    if (hitB > 0) { lastHitB = hitB; }
    if (hitC > 0) { lastHitC = hitC; }
    if (lastHitA > 0 && lastHitB > 0 && lastHitC > 0)
    {
      unsigned long first = min(hitTimeA, min(hitTimeB, hitTimeC));
      int tA = hitTimeA - first;
      int tB = hitTimeB - first;
      int tC = hitTimeC - first;
      Serial.print("A: ");
      Serial.print(tA);
      Serial.print(" B: ");
      Serial.print(tB);
      Serial.print(" C: ");
      Serial.println(tC);

      double center = score(0, 40, 32000, tA, tB, tC);
      double left = score(2090, 0, 1000, tA, tB, tC);
      double right = score(250, 0, 29000, tA, tB, tC);

      Serial.print("Center: ");
      Serial.println(center);
      Serial.print("Left: ");
      Serial.println(left);
      Serial.print("Right: ");
      Serial.println(right);
      

      //if (diffA - diffB < 1000)
      //{
      //  //Serial.println("LEFT");
      //  Serial.write((byte)0);
      //}
      //else
      //{
      //  //Serial.println("RIGHT");
      //  Serial.write((byte)1);
      //}
      //Serial.println("----------------------------------------------------------");
      //Serial.flush();
  #ifndef DEBUG
      int vel = max(max(lastHitA, lastHitB), lastHitC);
      //Serial.write(((double)min((vel - THRESHOLD) * GAIN, MAX) * 254 / RANGE) + 1.5); // 1 - 127
      Serial.flush();
      yield();
  #endif
      loopMode = 1; // wait for idle
    }
  }
  else if (loopMode == 1 /* wait for idle */ && stateA == 4 && stateB == 4 && stateC == 4) // all idle?
  {
    // reset everything for good measure
    lastHitA = lastHitB = lastHitC = 0;
    hitTimeA = hitTimeB = hitTimeC = 0;
    peakA = peakB = peakC = THRESHOLD;
    stateA = stateB = stateC = 0;
    lastHitA = lastHitB = lastHitC = 0;
    loopMode = 0; // back to waiting for hit
  }
}
#endif // PULLDATA

/*
ADC0	GP26	Pin 31 A
ADC1	GP27	Pin 32 B
ADC2	GP28	Pin 34 C

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
https://forum.arduino.cc/t/how-to-wire-a-piezo-as-an-impact-sensor/901726
*/