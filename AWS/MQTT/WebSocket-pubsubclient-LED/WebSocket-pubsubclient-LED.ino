//https://www.youtube.com/watch?v=doqLJIQagJY
//https://ap-southeast-1.console.aws.amazon.com/iot/home?region=ap-southeast-1#/thinghub

//NodeMcu ESP8266 is subscriber

#include <Arduino.h>
#include <Stream.h>

#include <ESP8266WiFi.h>
#include <ESP8266WiFiMulti.h>

//AWS
#include "sha256.h"
#include "Utils.h"

//WEBSockets
#include <Hash.h>
#include <WebSocketsClient.h>

//MQTT PUBSUBCLIENT LIB 
#include <PubSubClient.h>

//AWS MQTT Websocket
#include "Client.h"
#include "AWSWebSocketClient.h"
#include "CircularByteBuffer.h"

extern "C" {
  #include "user_interface.h"
}

//AWS IOT config, change these:
char wifi_ssid[]       = "CH3CHO + H2";
char wifi_password[]   = "ch3ch2oh";
char aws_endpoint[]    = "avt6g3kvwjn2o.iot.ap-southeast-1.amazonaws.com";
char aws_key[]         = "";
char aws_secret[]      = "";
char aws_region[]      = "ap-southeast-1";
const char* aws_topic  = "$aws/things/WebSocketPubsubclientLED/shadow/update";
int port = 443;

//MQTT config
const int maxMQTTpackageSize = 512;
const int maxMQTTMessageHandlers = 1;

ESP8266WiFiMulti WiFiMulti;

AWSWebSocketClient awsWSclient(1000);

PubSubClient client(awsWSclient);

//# of connections
long connection = 0;

//generate random mqtt clientID
char* generateClientID () {
  char* cID = new char[23]();
  for (int i=0; i<22; i+=1)
    cID[i]=(char)random(1, 256);
  return cID;
}

//count messages arrived
int arrivedcount = 0;

//callback to handle mqtt messages
void callback(char* topic, byte* payload, unsigned int length) {
  Serial.print("Message arrived [");
  Serial.print(topic);
  Serial.print("] ");
  
  for (int i = 0; i < length; i++) {
    Serial.print((char)payload[i]);
  }
  Serial.println();
  
  SwitchLed(payload);
  SendStateLed();
}
void SwitchLed(byte* payload)
{
  // Switch on the LED if an 1 was received as first character
  Serial.print("payload[0] = "); Serial.println((char)payload[0]);
  if ((char)payload[0] == '1') {
    digitalWrite(BUILTIN_LED, LOW);   // Turn the LED on (Note that LOW is the voltage level
    // but actually the LED is on; this is because
    // it is acive low on the ESP-01)
  } else {
    digitalWrite(BUILTIN_LED, HIGH);  // Turn the LED off by making the voltage HIGH
  }
}
void SendStateLed()
{    
  if (connect ()){
      subscribe ();

      String stateLed;
      if(digitalRead(BUILTIN_LED) == 1)
        stateLed = "OFF";
      else stateLed = "ON";
      sendmessage(stateLed);
  }
}

//connects to websocket layer and mqtt layer
bool connect () {
    //Connect to Server
    if (client.connected()) {    
        client.disconnect ();
    }  
    //delay is not necessary... it just help us to get a "trustful" heap space value
    delay (1000);
    Serial.print (millis ());
    Serial.print (" - conn: ");
    Serial.print (++connection);
    Serial.print (" - (");
    Serial.print (ESP.getFreeHeap ());
    Serial.println (")");

    //creating random client id
    char* clientID = generateClientID ();
    
    client.setServer(aws_endpoint, port);
    if (client.connect(clientID)) {
      Serial.println("connected");     
      return true;
    } else {
      Serial.print("failed, rc=");
      Serial.print(client.state());
      return false;
    }    
}

//subscribe to a mqtt topic
void subscribe () {
    client.setCallback(callback);
    client.subscribe(aws_topic);
   //subscript to a topic
    Serial.println("MQTT subscribed");
}

//send a message to a mqtt topic
void sendmessage (String content) {
    char buf[100];
    //strcpy(buf, "{\"state\":{\"reported\":{\"on\": false}, \"desired\":{\"on\": true}}}");   
    String mess = "{\"state\":{\"reported\":{\"LED\": \"" + content + "\"}}}";
    strcpy(buf, mess.c_str());
    int rc = client.publish(aws_topic, buf); 
    //Serial.print("client.publish: "); Serial.println(mess);   
}


void setup() {
    pinMode(BUILTIN_LED, OUTPUT);     // Initialize the BUILTIN_LED pin as an output
    digitalWrite(BUILTIN_LED, 1);
    
    wifi_set_sleep_type(NONE_SLEEP_T);
    Serial.begin (115200);
    delay (2000);
    Serial.setDebugOutput(1);

    //fill with ssid and wifi password
    WiFiMulti.addAP(wifi_ssid, wifi_password);
    Serial.println ("connecting to wifi");
    while(WiFiMulti.run() != WL_CONNECTED) {
        delay(100);
        Serial.print (".");
    }
    Serial.println ("\nWIFI is connected");

    //fill AWS parameters    
    awsWSclient.setAWSRegion(aws_region);
    awsWSclient.setAWSDomain(aws_endpoint);
    awsWSclient.setAWSKeyID(aws_key);
    awsWSclient.setAWSSecretKey(aws_secret);
    awsWSclient.setUseSSL(true);

    SendStateLed();
}

void loop() {
  //keep the mqtt up and running
  if (awsWSclient.connected ()) {    
      client.loop ();
  } else {
    //handle reconnection
    if (connect ()){
      subscribe ();      
    }
  }

}
