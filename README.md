[中文](https://github.com/dathlin/HslCommunication/blob/master/docs/Chinese.md)
<pre>
             ///\      ///\             /////////\              ///\
            //\\/      //\/           //\\\\\\\\//\            //\\/
           //\/       //\/          //\\/       \\/           //\/
          //\/       //\/           \//\                     //\/
         /////////////\/             \//////\               //\/
        //\\\\\\\\\//\/               \\\\\//\             //\/
       //\/       //\/                     \//\           //\/
      //\/       //\/           ///\      //\\/          //\/       //\
     ///\      ///\/            \/////////\\/           /////////////\/
     \\\/      \\\/              \\\\\\\\\/             \\\\\\\\\\\\\/             Present by Richard.Hu
</pre>

# HslCommunication
HslCommnication.dll
![Build status](https://img.shields.io/badge/Build-Success-green.svg) [![NuGet Status](https://img.shields.io/nuget/v/HslCommunication.svg)](https://www.nuget.org/packages/HslCommunication/) ![NuGet Download](https://img.shields.io/nuget/dt/HslCommunication.svg) [![Gitter](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/HslCommunication/community) [![NetFramework](https://img.shields.io/badge/Language-C%23%207.0-orange.svg)](https://blogs.msdn.microsoft.com/dotnet/2016/08/24/whats-new-in-csharp-7-0/) [![Visual Studio](https://img.shields.io/badge/Visual%20Studio-2019-red.svg)](https://www.visualstudio.com/zh-hans/) ![copyright status](https://img.shields.io/badge/CopyRight-Richard.Hu-brightgreen.svg) 

HslCommunication.jar
![Build status](https://img.shields.io/badge/Build-Success-green.svg) ![NetFramework](https://img.shields.io/badge/Language-java-orange.svg) ![JDK status](https://img.shields.io/badge/JDK-1.8.0-green.svg) ![IDE status](https://img.shields.io/badge/Intellij%20Idea-2018.4-red.svg) ![copyright status](https://img.shields.io/badge/CopyRight-Richard.Hu-brightgreen.svg) 

HslCommunication.py
![Build status](https://img.shields.io/badge/Build-Success-green.svg) ![License status](https://img.shields.io/badge/License-LGPL3.0-yellow.svg) ![NetFramework](https://img.shields.io/badge/python-3.6-orange.svg) ![IDE status](https://img.shields.io/badge/Visual%20Studio-Code-red.svg) ![copyright status](https://img.shields.io/badge/CopyRight-Richard.Hu-brightgreen.svg) 

## CopyRight
(C) 2017 - 2020 Richard.Hu, All Rights Reserved

## Active
<pre>
// Active Example
if(!HslCommunication.Authorization.SetAuthorizationCode( "Your Code" ))
{
    // MessageBox.Show( "Active Failed! only can use dll on 8 hours!" );
    // return;
}
</pre>

## Official Website
Webside: [http://www.hslcommunication.cn/](http://www.hslcommunication.cn/)

BBS: [http://bbs.hslcommunication.cn/](http://bbs.hslcommunication.cn/)

API: [http://api.hslcommunication.cn/](http://api.hslcommunication.cn/)

Gitter[talk with me]: [https://gitter.im/HslCommunication/community](https://gitter.im/HslCommunication/community)

## What is HSL
This is an industrial IoT based, computer communications architecture implementation, integrated with most of the basic functional implementation of industrial software development, 
such as Mitsubishi PLC Communications, Siemens PLC Communications, OMRON PLC Communications, Modbus Communications,
All of these communications have been implemented in multiple languages, and of course, the feature integration of the main. NET Library is even more powerful, 
in addition to the implementation of cross-program, cross-language, cross-platform communication, so that you are no longer obsessed with the use of Windows or Linux system, 
the realization of log function, flow number generation function, mail sending function, Fourier transform function, and so on, 
will integrate more common features of industrial environment in the future.

In order not to let the industry 4.0 stay on the slogan, the high-rise flat up, and the cornerstone is HSL.

## What can HSL do
HSL can connect the equipment of the industrial production site to the free transmission of data at the bottom, whether active or passive, 
whatever your acquisition system (usually the acquisition system is a Windows computer, or an embedded system, or a Linux-based box),
can achieve the random transmission of data, convenient and fast to achieve a strong, real-time, high-response robust system, whether you are building a C/S system, 
or B/S system, or C-B-S-A (Integrated desktop client, browser, Android) hybrid system, is a fast and low-cost implementation,

As long as you have the primary data of the industrial field, that is, can build a powerful real-time monitoring function of the software,
production reports and automated scheduling software, a variety of process parameters history tracking software, data based on the experience of machine learning software, 
as well as full-featured MES system and so on. 

**By the way**, the traditional industrial model is the procurement of off-the-shelf industrial software, 
including the host computer software and MES system, while ignoring their own system.
For some industry-standard functional software, such as ERP systems, financial software, these can be purchased directly,
However, for the host computer and MES system, the actual needs of each enterprise are very different, it is difficult to have a common scene, 
and the current situation is to spend a lot of money to do small things, so here, give a future-oriented model to achieve: for the production enterprise, 
Based on HSL to develop enterprise-class MES system implementation, as the core Warehouse center of data, and business logic processing Center, 
for equipment suppliers, based on HSL to develop the host computer software system, fast and convenient distribution of data to the customer's MES system, work together.

## Install From NuGet
Description: NuGet for stable version, Support Online upgrade, the use of components is best downloaded from NuGet, 
the project published here is likely to have not yet compiled the beta version, NuGet installation is as follows:
```
Install-Package HslCommunication
```

## Environment
* IDE: **Visual Studio 2019** 
* java：**Intellij Idea 2018.4**
* python: **Visual Studio Code**

## Contact
* Email: hsl200909@163.com
* ![reward](https://raw.githubusercontent.com/dathlin/HslCommunication/master/imgs/support.png)

## Supported Model and price [welcome to complete]
#### Siemens
No. | Class | Model | Price | Remarks | Contributor
-|-|-|-|-|-
1 | ![pic](https://img.shields.io/badge/-SiemensS7Net-informational.svg) | ![pic](https://img.shields.io/badge/1215C-Good-success.svg) | ￥ | &nbsp; | [Richard Hu](https://github.com/dathlin/HslCommunication)

#### Melsec
No. | Class | Model | Price | Remarks | Contributor
-|-|-|-|-|-
1 | ![pic](https://img.shields.io/badge/-MelsecMcNet-informational.svg) | ![pic](https://img.shields.io/badge/QJ71E71%20100-Good-success.svg) | ￥ | &nbsp; | [Richard Hu](https://github.com/dathlin/HslCommunication)
2 | ![pic](https://img.shields.io/badge/-MelsecMcNet-informational.svg) | ![pic](https://img.shields.io/badge/Q02H-Good-success.svg) | ￥ | &nbsp; | [Richard Hu](https://github.com/dathlin/HslCommunication)
3 | ![pic](https://img.shields.io/badge/-MelsecMcNet-informational.svg) | ![pic](https://img.shields.io/badge/L02H-Good-success.svg) | ￥ | &nbsp; | [Richard Hu](https://github.com/dathlin/HslCommunication)
4 | ![pic](https://img.shields.io/badge/-MelsecMcNet-informational.svg) | ![pic](https://img.shields.io/badge/Fx5u-Good-success.svg) | ￥ | &nbsp; | [Richard Hu](https://github.com/dathlin/HslCommunication)

#### AB plc
No. | Class | Model | Price | Remarks | Contributor
-|-|-|-|-|-
1 | ![pic](https://img.shields.io/badge/-AllenBradleyNet-informational.svg) | ![pic](https://img.shields.io/badge/1769-Good-success.svg) | ￥ | &nbsp; | &nbsp;
1 | ![pic](https://img.shields.io/badge/-AllenBradleyNet-informational.svg) | ![pic](https://img.shields.io/badge/1756-Good-success.svg) | ￥ | &nbsp; | &nbsp;

#### Omron
No. | Class | Model | Price | Remarks | Contributor
-|-|-|-|-|-
1 | ![pic](https://img.shields.io/badge/-OmronFinsNet-informational.svg) | - | ￥ | &nbsp;| &nbsp;

#### Keyence
No. | Class | Model | Price | Remarks | Contributor
-|-|-|-|-|-
1 | ![pic](https://img.shields.io/badge/-OmronFinsNet-informational.svg) | - | ￥ | &nbsp;| &nbsp;

#### ModbusTcp
No. | Class | Model | Price | Remarks | Contributor
-|-|-|-|-|-
1 | ![pic](https://img.shields.io/badge/-Modbus%20Tcp-informational.svg) | ![pic](https://img.shields.io/badge/1215C-Good-success.svg) | ￥ | &nbsp; | [Richard Hu](https://github.com/dathlin/HslCommunication)
4 | ![pic](https://img.shields.io/badge/-Modbus%20Tcp-informational.svg) | ![pic](https://img.shields.io/badge/Fx5u-Good-success.svg) | ￥ | &nbsp; | [Richard Hu](https://github.com/dathlin/HslCommunication)

#### Panasonic
No. | Class | Model | Price | Remarks | Contributor
-|-|-|-|-|-
1 | ![pic](https://img.shields.io/badge/-PanasonicMcNet-informational.svg) | ![pic](https://img.shields.io/badge/FP7%20CPS31E-Good-success.svg) | ￥ | &nbsp;| 镇江-Relax;


## HslCommunication.dll Summary 
When I started working on this project, I had an idea of how to easily and quickly read and write PLC data. Our code logic should be very simple, 
and it only takes one or two lines of code to implement this feature. Like this
```
// Pseudo code
PLC plc = new PLC("192.168.0.11", 6000);

short value = plc.ReadInt16("D100");
```
But after a long period of development and attempt, found that the return of PLC is likely to be abnormal, this anomaly may come from the network failure, 
may also come from you entered the wrong address, or the PLC itself is not allowed to operate, so in this project added a class **Operateresult**, 
So the final code becomes what it looks like (with Siemens PLC as an example)
```
SiemensS7Net siemens = new SiemensS7Net( SiemensPLCS.S1200, " 192.168.1.110" );
OperateResult<short> read = siemens.ReadInt16("M100");

if(read.IsSuccess)
{
	// you get the right value
	short value = read.Content;
}
else
{
	// failed , but you still can know the failed detail
	Consolo.WriteLine(read.Message);
}
```
Of course, you can also write very concise, because the judgment of success is ignored, so the following operation is risky.
```
SiemensS7Net siemens = new SiemensS7Net( SiemensPLCS.S1200, " 192.168.1.110" );
short value = siemens.ReadInt16("M100").Content;   // Look at this code, isn't it very succinct.
```

The above operation we have read the data, but is based on a short connection, 
when the reading of the data finished, automatically shut down the network, 
if you want to open a long connection, follow the following actions.

```
SiemensS7Net siemens = new SiemensS7Net( SiemensPLCS.S1200, " 192.168.1.110" );
siemens.SetPersistentConnection( );
OperateResult<short> read = siemens.ReadInt16("M100");

if(read.IsSuccess)
{
	// you get the right value
	short value = read.Content;
}
else
{
	// failed , but you still can know the failed detail
	Consolo.WriteLine(read.Message);
}

// when you don't want read data, you should call close method
siemens.ConnectClose( );

```

So we can see that all the other modes of communication are similar to this, including Mitsubishi PLC, Siemens PLC,AB PLC, OMRON PLC, Keane plc, Panasonic Plc,
redis Communications, EFT Robots, Kuka robots and so on, including its own support for the HSL protocol.

The goal is to reduce the cost of learning for developers, and usually you have to learn how to use several different libraries and learn the basics of PLC. Now, 
all you need to know is how the basic PLC address is represented, and you can read and write PLC data.


Called from Visual C++ project

cppProject -> Properties -> Configuration Properties -> General -> CLR Support

Add HslCommunication.dll(net35) reference
```
#include "pch.h"
#include <iostream>
using namespace HslCommunication;
using namespace ModBus;

int main()
{
    std::cout << "Hello World!\n";


	// This is the demo , called C# ModbusTcpNet
	System::String ^ipAddress = gcnew System::String("127.0.0.1");
	ModbusTcpNet ^modbus = gcnew ModbusTcpNet(ipAddress, 502, 1);

	System::String ^dataAddress = gcnew System::String("100");
	OperateResult<short> ^readValue = modbus->ReadInt16(dataAddress);
	if (readValue->IsSuccess) {
		short value = readValue->Content;
		printf("Read Value：%d \n", value);
	}
	else
	{
		printf("Read Failed");
	}
}
```

If you want to communication in your mobile phone application, you also can use C# code by xamarin, you can download HslAppDemo to test
[HslAppDemo.apk](https://github.com/dathlin/HslCommunication/raw/master/Download/com.companyname.HslAppDemo-Signed.apk)


Another feature of this project is support for cross-language communication support. You can build a C # background server that supports Windows desktop application 
and Web background, and Android phone-side, Python programs, Java programs to communicate. server side code:
```
class Program
{
    static void Main(string[] args)
    {
		NetSimplifyServer simplifyServer;
		try
		{
			simplifyServer = new NetSimplifyServer( );
			simplifyServer.ReceiveStringEvent += SimplifyServer_ReceiveStringEvent;
			simplifyServer.ServerStart( 12345 );
		}
		catch(Exception ex )
		{
			Console.WriteLine( "Create failed: " + ex.Message );
			Return;
		}

		Console.ReadLine();
	}

	private static void SimplifyServer_ReceiveStringEvent( AppSession session, NetHandle handle, string value )
	{
		if (handle == 1)
		{
			// Message to operate when a signal from the client is received 1
			simplifyServer.SendMessage( session, handle, "This is test single：" + value );
		}
		else
		{
			simplifyServer.SendMessage( session, handle, "not supported msg" );
		}
	
		// Show out, who sent it, what did it send?
		Console.WriteLine($"{session} [{handle}] {value}");
	}
}
```
C# Client Side (Also asp.net mvc, asp.net core mvc)
```
NetSimplifyClient simplifyClient = new NetSimplifyClient( "127.0.0.1", 12345 );
string value = simplifyClient.ReadFromServer( 1, "test" ).Content;
```
Java Client Side
```
NetSimplifyClient simplifyClient = new NetSimplifyClient( "127.0.0.1", 12345 );
string value = simplifyClient.ReadFromServer( 1, "test" ).Content;
```
Python Client Side
```
netSimplifyClient = NetSimplifyClient("127.0.0.1",12345)
value = netSimplifyClient.ReadFromServer(1,'123').Content
```

**Note**: In the source code, still contains a lot of Chinese annotation, in the future for a short period of time, 
will be used in English and Chinese double annotation, thank you for your understanding.

**HslCommunicationDemo** The features supported by this project can be roughly clear through the demo interface below:
![Picture](https://raw.githubusercontent.com/dathlin/HslCommunication/master/imgs/demo.png)


## HslCommunication.jar Summary 
This component provides the Java version, for the. NET version of the castration version, removed all the server function code, 
retained part of the client function code, convenient and plc, device data interaction, and C # program data interaction, 
this jar component is suitable for the development of Android, easy to build a. NET Server + Windows Client + asp.net client + J2EE client + Java Client + Android client.
![Picture](https://raw.githubusercontent.com/dathlin/HslCommunication/master/imgs/java_demo.png)

## HslCommunication.py Summary 
This component provides a Python version, a castration version of the. NET version, removes all server function codes, retains some of the client function code, 
facilitates data interaction with PLC, devices, and data interaction with C # programs for cross-platform operation

## Xamarin.Android Demo
![Picture](https://raw.githubusercontent.com/dathlin/HslCommunication/master/imgs/appDemo.png)

## Contribution
Warmly welcome suggestions for improvement of the Code of this project, you can launch pull Request.

## Thanks
* Hybrid locks and serializable exception classes, read and write locks, concurrent model parts code and ideas refer to "CLR Via C #", thanks to the author Jeffrey Richter

## Cooperation Company
![Picture](https://raw.githubusercontent.com/dathlin/HslCommunication/master/imgs/partner/woody.png) [![Picture](https://raw.githubusercontent.com/dathlin/HslCommunication/master/imgs/partner/均达电气.png)](http://junda-jy.com.cn/)
[![Picture](https://raw.githubusercontent.com/dathlin/HslCommunication/master/imgs/partner/Pia.jpg)](http://www.piagroup.com) [![Picture](https://raw.githubusercontent.com/dathlin/HslCommunication/master/imgs/partner/堤摩讯.png)](https://www.timotion.com)
[![Picture](https://raw.githubusercontent.com/dathlin/HslCommunication/master/imgs/partner/盛意达.jpg)](http://bjsyd.cn) [![Picture](https://raw.githubusercontent.com/dathlin/HslCommunication/master/imgs/partner/PENC.jpg)](http://www.penc.com)
![Picture](https://raw.githubusercontent.com/dathlin/HslCommunication/master/imgs/partner/rocket_Blue.png) ![Picture](https://raw.githubusercontent.com/dathlin/HslCommunication/master/imgs/partner/翔宇自控.jpg) 
[![Picture](https://raw.githubusercontent.com/dathlin/HslCommunication/master/imgs/partner/杭州优海.png)](http://www.eohi700.com/)

## Controls
This library include some controls render upside picture. u can easily use them
![Picture](https://raw.githubusercontent.com/dathlin/HslCommunication/master/imgs/controls.png)

## Mitsubishi PLC Communication
Using MC protocol, Qna 3E, Include binary and ascii
![Picture](https://raw.githubusercontent.com/dathlin/HslCommunication/master/imgs/Melsec1.png)

## Siemens PLC Communication
Using S7 protocol And Fetch/Write protocol
![Picture](https://raw.githubusercontent.com/dathlin/HslCommunication/master/imgs/siemens1.png)

## Omron PLC Communication
Using Fins-tcp protocol
![Picture](https://raw.githubusercontent.com/dathlin/HslCommunication/master/imgs/Omron.png)

## AllenBradley PLC Communication
![Picture](https://raw.githubusercontent.com/dathlin/HslCommunication/master/imgs/ab1.png)

## Modbus-tcp Communication
Client, using read/write coils and register, read discrete input , read register input
![Picture](https://raw.githubusercontent.com/dathlin/HslCommunication/master/imgs/Modbus1.png)

Server, you can build your own modbus-tcp server easily
![Picture](https://raw.githubusercontent.com/dathlin/HslCommunication/master/imgs/Modbus2.png)

## Redis Client Communication
![Picture](https://raw.githubusercontent.com/dathlin/HslCommunication/master/imgs/redis.png)

## Simplify Net [ Based on Tcp/Ip ]
Communicaion with multi-computers , client can exchange data with server easily, include server side ,client side

![Picture](https://raw.githubusercontent.com/dathlin/HslCommunication/master/imgs/Simlify1.png)
![Picture](https://raw.githubusercontent.com/dathlin/HslCommunication/master/imgs/Simlify2.png)

## Udp Net [ Base on Udp/Ip ]
Communicaion with multi-computers , client can send a large of data to server, include server side ,client side

![Picture](https://raw.githubusercontent.com/dathlin/HslCommunication/master/imgs/Udp1.png)
![Picture](https://raw.githubusercontent.com/dathlin/HslCommunication/master/imgs/Udp2.png)

## File Net [ Base on Tcp/Ip ]
Communicaion with multi-computers , client can exchange File with server easily, include server side ,client side

![Picture](https://raw.githubusercontent.com/dathlin/HslCommunication/master/imgs/File1.png)
![Picture](https://raw.githubusercontent.com/dathlin/HslCommunication/master/imgs/File2.png)
