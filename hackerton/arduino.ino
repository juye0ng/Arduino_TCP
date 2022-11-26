#include <MFRC522.h>
#include <Servo.h>

#define motor 2

Servo servo;

void setup()
{
	Serial.begin(115200);
    Serial.println("tid");
    /*servo.attach(motor);
    stop();*/
    Serial.println("tlqkffusdk");
}

void loop()
{
	/*if(Serial.available()){
        String str=Serial.readString();
        if(str=="stop"){
            stop();
        }
        if(str=="run"){
            run();
        }
    }*/
}

/*void stop(){
    servo.write(45);
    Serial.println("stop");
}
void run(){
    servo.write(135);
    Serial.println("run");
}*/