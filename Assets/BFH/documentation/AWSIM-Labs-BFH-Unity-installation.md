# Installation Guide for the AWSIM-Labs-BFH-Unity project
This guide is for the installation of the Unity project _AWSIM Labs BFH_, after cloning it.

 Although [_AWSIM_'s setup guide](https://autowarefoundation.github.io/AWSIM-Labs/main/GettingStarted/SetupUnityProject/) is already quite sufficient, it does not cover all the Unity Assets used in _AWSIM Labs BFH_ and the _Unity Editor version differs. 
It's recommended to follow this document with the official guide at hand.

>**Note:**
>This guide was created for an _AWSIM Labs ver. 1.5.3_ fork, using the _Unity Editor ver. 2022.3.50f1_, and the _Nvidia_ driver 560. 
>Review the [Edit History](README-bfh.md) for further information.
>
## Befor you begin
Since this project - _AWSIM Labs BFH_ - is a fork of _AWSIM Labs_, the `main` branch is still used for merging for maintaining the project (more about that [here](project-maintainance.md)).

For that reason, _AWSIM Labs BFH_'s modifications to the project are only available in the `bfh` branch (and branches derived from it).

Because the fork and the original use a different _Unity Editor_ and have different Assets, please switch to the `bfh` branch befor setting up the project according to this guide.

## Prerequisits

## Git
If you haven't already installed `git`, consult the [_Autoware Installation_ guide](autoware-installation.md). It contains a section describing the installation and setup of `git`.

## Nvidia Driver installation
>**Note:**
>This step is only necessary if you haven't already [installed _Autoware_](autoware-installation.md).

To ensure compatability, install the following _Nvidia_ driver vers. `560`, for which this project has been tested.

>**Note:**
>If you have reasons not to install (another)_Nvidia_ driver on your system, you can skip this part and setup the _Unity_ project anyway. **However**, it will probably not work properly in combination with Autoware.

### Check your hardware:
Run this command to see if your system recognizes the graphics card:
```sh
nvidia-smi
```
>**Note:**:
>If the command shows you that the driver `560` is already installed, you can skip this step.

### Nvidia drivers repository
Ensure you have the latest _Nvidia_drivers repository:
```sh
sudo add-apt-repository ppa:graphics-drivers/ppa
sudo apt update
```
### Check available drivers
Verify the availability of driver `560`:
```sh
ubuntu-dirvers devices
```
### Install the driver

```sh
sudo apt install nvidia-driver-560
```
>**Note:**
>If you couldn't find the `560` driver, try installing another driver, with a version number that is istll relatively close to `560`.
>**BUT**, project might not work with _Autoware_.

### Reboot and verify
Reboot your system and verify the installation with the command
```sh
nvidia-smi
```
## Make Network configurations for DDS

Follow the instruction on [this page](https://autowarefoundation.github.io/AWSIM-Labs/main/GettingStarted/QuickStartDemo/#dds-configuration), i.o. to setup the network configuratons to use `DDS`. 

The instructions there are:
### DDS configurations
Add the following lines to `~/.bashrc `file:
```
if [ ! -e /tmp/cycloneDDS_configured ]; then
    sudo sysctl -w net.core.rmem_max=2147483647
    sudo sysctl -w net.ipv4.ipfrag_time=3
    sudo sysctl -w net.ipv4.ipfrag_high_thresh=134217728     # (128 MB)
    sudo ip link set lo multicast on
    touch /tmp/cycloneDDS_configured
fi
```
Every time you restart this machine, and open a new terminal, the above commands will be executed.

Until you restart the machine, they will not be executed again.

### CycloneDDS configuration
Save the following as `cyclonedds.xml` in your home directory `~`:

~~~
<?xml version="1.0" encoding="UTF-8" ?>
<CycloneDDS xmlns="https://cdds.io/config" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:schemaLocation="https://cdds.io/config https://raw.githubusercontent.com/eclipse-cyclonedds/cyclonedds/master/etc/cyclonedds.xsd">
    <Domain Id="any">
        <General>
            <Interfaces>
                <NetworkInterface name="lo" priority="default" multicast="default" />
            </Interfaces>
            <AllowMulticast>default</AllowMulticast>
            <MaxMessageSize>65500B</MaxMessageSize>
        </General>
        <Internal>
            <SocketReceiveBufferSize min="10MB"/>
            <Watermarks>
                <WhcHigh>500kB</WhcHigh>
            </Watermarks>
        </Internal>
    </Domain>
</CycloneDDS>
~~~

Make sure the following lines are added to the `~/.bashrc` file:
```sh
export RMW_IMPLEMENTATION=rmw_cyclonedds_cpp
export CYCLONEDDS_URI=/home/your_username/cyclonedds.xml
```
Replace `your_username `with your actual username.

## Install Unity Hub
In this guide, _Unity Hub_ will be installed with `snap`.

### Install snap (if not installed):
```sh
sudo apt isntall snap
```

### Install Unity Hub with snap
```sh
sudo snap install unityhub --classic
```
### Start Unity Hub
you can start the _Unity Hub_ with 

```sh
unityhub
```
## Install Unity Editor
_AWSIM Labs BFH_ runs with the _Unity Editor_ ver. `2022.3.50f1`, contrary to the version `2022.3.36f1` in _AWSIM Labs_.

To install the Editor version `2022.3.50f1`, open the _Unity Hub_ as described above and install the version through the menues `Installs`->`Install Editor`. If you can't find the version under `Official Releases`, you might find it in the `Archive` section.

After finding the version, click the `download/install` button to start the installation process. - At this point, your Unity installation process should have started.

## Login and prepare Assets
_AWSIM Labs_ uses the Assets [`Vehicle Physics Pro`](https://assetstore.unity.com/packages/tools/physics/vehicle-physics-pro-community-edition-153556), and [`Graphy`](https://assetstore.unity.com/packages/tools/gui/graphy-ultimate-fps-counter-stats-monitor-debugger-105778).
_AWSIM Labs BFH_ adds [`Modular Lowpoly Streets`](https://assetstore.unity.com/packages/3d/environments/urban/modular-lowpoly-streets-free-192094) to the project.

All these Assets are free and should be added to `My Assets` in _Unity_'s _Asset Store_, in order to donwload and import them directly with the _Unity Editor_.

Make sure to add these Assets to `My Assets`, and login at the _Unity Hub_ with your _Unity_ account.


## Open the AWSIM Labs BFH project
>**Note:**
>Ensure that you've already switched to the `bfh` branch at this point.

>**Note:**
>Make sure you don't have _ROS2_ sourced in the terminal window you're using in this step. This will interfere with _AWSIM Lab_'s own instance of _ROS2_ ([`ros2-for-unity`](https://github.com/RobotecAI/ros2-for-unity)).

In a terminal, navigate to the directory, where the _Unity_ project is installed.

Run the following command to start the _Unity_ project:

```sh
~/Unity/Hub/Editor/2022.3.50f1/Editor/Unity -projectPath .
```

### Import missing Assets
When starting the project the first time, you probably get the _safe mode dialog_ upon start up.
The official guide suggest that the cause of this is a missing dependec (`openssl`) and installing it would resolve it. However, from what was experienced during this project, this was simply not the case.

If the _safe mode_ dialog opens, enter _safe mode_, but first make sure you're in the branch `bfh`.

_Safe mode_ or not, the project should be open now, and ready for the Assets (`Vehicle Physics Pro`, `Graphy`, and `Modular Lowpoly Streets`) to be installed.

Install the assets accoring to [_Unity_'s documentation](https://docs.unity3d.com/2022.3/Documentation/Manual/upm-ui-import.html).

If you entered _safe mode_, the editor switches automatically after importing the Assets.

### Import External Packages
To properly run and use AWSIM project in Unity it is required to download map package which is not included in the repository.

Follow the instructions from the official guide, [here](https://autowarefoundation.github.io/AWSIM-Labs/main/GettingStarted/SetupUnityProject/#import-external-packages).


## Test Installation
In case you also have _Autoware_ installed and have set up the _Autoware_ workspace successfully, you can test your installation of _AWSIM Labs BFH_ be running the [`Quick Start Demo`](https://autowarefoundation.github.io/AWSIM-Labs/main/GettingStarted/QuickStartDemo/). 

In case you run into any problems, check out the [Troubleshooting page](https://autowarefoundation.github.io/AWSIM-Labs/main/DeveloperGuide/TroubleShooting/).

