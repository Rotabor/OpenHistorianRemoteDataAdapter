# OpenHistorianDataRemotingAdapter
The OpenHistorian (https://github.com/GridProtectionAlliance/openHistorian) universal adapter to acquire data from any remote data source server over .NET Remoting.

The typical application is OPC DA (https://github.com/Rotabor/OpcDaAgent). Using with OPC DA remote server, this adapter helps to resolve access rights conflict and 32/64bit incompatibility.

**Concept**

![openHistorian Web Interface](https://github.com/Rotabor/OpenHistorianRemoteDataAdapter/blob/master/GitHubResources/OpenHistorianDataRemotingAdapter.png)

**Disclaimer**: This code is provided as is, without any warranty or obligation. It requires you to have knowledge of C# programming, openHistorian and other products/libraries/technologies in use. It has to be compiled.
This code is created with help of OpenHistorian-OPC-UA-Adapter-master (https://github.com/Pinkisagit/OpenHistorian-OPC-UA-Adapter).

## How to configure:
1. Compile source code - refer to GSF dlls provided by the installed OpenHistorian. The target platform should match openHistorian's platform - .Net Framework 4.8 64bit for openHistorian 2.8.157.
2. Create a folder within OpenHistorian folder called DataRemoting
3. Copy all files from bin directory into DataRemoting folder
4. Open the historian database using whichever SQL server you have chosen
5. Add new entry to Protocol table
    - **Acronym**: DataRemoting
    - **Name**: Data Remoting Adapted
    - **Type**: Measurement
    - **Category**: Device
    - **AssemblyName**: DataRemoting\OpenHistorianOPCDAAdapter.dll
    - **TypeName**: nsOpenHistorianRemoteDataAdapter.RemoteDataAdapter
6.  In OpenHistorian (you may need to restart it), create a new device
    - **Protocol**: select DataRemoting Adapter from the list
    - **Connection String**: "remotehost=THEHOST;port=XXXXX;renewaltime=YY"
    - Make sure to fill Historian field and other compulsory fields
    - Check **Enabled**
    - Save
7. Add a new measurement
    - **Point Tag**: meaningful name
    - **Signal Reference**: the correct name for the remote data server
    - **Device**: Select device you have created in Step 6
    - **Measurement Type**: Analog (if the tag is a real)
    - Fill in **Description** and **Historian**
    - Check **Enabled**
    - Save
8. Restart OpenHistorian service.
9. Examine the log file to see if there are any errors.
