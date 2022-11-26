#include <SoftwareSerial.h>

#define txpin 2
#define rxpin 3
#define echo 13
#define trig 12
#define RED 10
#define YELLOW 9
#define GREEN 8

SoftwareSerial mySerial(txpin, rxpin);

bool isRed = false;


void setup() {
  pinMode(RED, OUTPUT);
  pinMode(YELLOW, OUTPUT);
  pinMode(GREEN, OUTPUT);
  pinMode(echo, INPUT);
  pinMode(trig, OUTPUT);
  Serial.begin(9600);
  mySerial.begin(9600);
}

void REDm() {
  digitalWrite(RED, HIGH);
  digitalWrite(YELLOW, LOW);
  digitalWrite(GREEN, LOW);
}

void GREENm() {
  digitalWrite(RED, LOW);
  digitalWrite(YELLOW, LOW);
  digitalWrite(GREEN, HIGH);
  isRed = false;
}

void YELLOWm() {
  digitalWrite(RED, LOW);
  digitalWrite(YELLOW, HIGH);
  digitalWrite(GREEN, LOW);
  isRed = true;
}

void loop() {

  if (mySerial.available()) { //블루투스에서 넘어온 데이터가 있다면
    String txt = mySerial.readString();
    if(txt == "RED") {
      //직전 노란불 1초간
      YELLOWm();
      delay(1000);

      REDm();
      Serial.println(txt);
    }

    else if(txt == "GREEN") {
      GREENm();
      Serial.println(txt);
    }
  }

  digitalWrite(trig, LOW);
  digitalWrite(echo, LOW);
  delayMicroseconds(2);
  digitalWrite(trig, HIGH);
  delayMicroseconds(10);
  digitalWrite(trig, LOW);
 
  unsigned long duration = pulseIn(echo, HIGH);
 
  float distance = duration / 29.0 / 2.0;
 
  Serial.print(distance);
  Serial.println("cm");

  if(distance<20) {
    if(isRed==true) {
      mySerial.write("RED");
    }
    else mySerial.write("GREEN");
  }
  
  delay(100);
  digitalWrite(echo, LOW);
}