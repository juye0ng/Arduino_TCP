#include <Servo.h>
#include <SoftwareSerial.h>
#define motor 2

Servo servo;
SoftwareSerial softSerial(4,3);

void setup()
{
  Serial.begin(9600);
  softSerial.begin(9600);
  
  servo.attach(motor);
  //stop();
}

bool green=false;

void loop()
{
  if(softSerial.available()){
    //Serial.write(softSerial.read());
  }
  if(softSerial.available()){
    //softSerial.write(Serial.read());
    String str=softSerial.readString();
        Serial.println(str);
        //String str=Serial.readString();
        if(str=="GREEN"&&!green){
            run();
            green=true;
        }
        if(str=="RED"&&green){
            green=false;
            stop();
        }
  }
  /*if(Serial.available()){x
        //String str=Serial.readString();
        
    }*/
    
}

void stop(){
    servo.write(45);
    Serial.println("stop");
}
void run(){
    servo.write(150);
    Serial.println("run");
}
