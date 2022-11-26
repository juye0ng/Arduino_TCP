#define red 5
#define yellow 6
#define green 7

void setup() {
  Serial.begin(9600);
}

void off(){
  digitalWrite(red, LOW);
  digitalWrite(green, LOW);
  digitalWrite(yellow, LOW);
}

void loop() {
  off();
  Serial.println("red");
  digitalWrite(red, HIGH);
  delay(500);
  off();
  Serial.println("green");
  digitalWrite(green, HIGH);
  delay(500);
  off();
  Serial.println("yellow");
  digitalWrite(yellow, HIGH);
  delay(500);
}
