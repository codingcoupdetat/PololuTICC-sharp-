# PololuTICC-sharp-
a C# libarary for the pololu tic steppermotor driver for the raspberrypi (using mono and wiringpi library)

This is a simple pololu tic steppermotor driver written in C# for use with the raspberry-pi and mono.
To use this library you must install wiringpi (found here http://wiringpi.com/)

There is some added features using reflection so you can easily shield the properties of the driver by setting a attribute above the property
and using the Properties property to fetch them.
Usage, create a instance of PololuTicI2C with the address byte set to the address of the tic (this can be set/viewed using the pololutic windows software and connecting it with the usb port to your computer)

this is my first git upload so it leaves a lot to be desired, in anycase please feel free to contact me with suggestions/questions.
