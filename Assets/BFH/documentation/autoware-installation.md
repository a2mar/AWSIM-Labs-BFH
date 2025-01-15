# Autoware Installation

This guide provides a streamlined overview to assist in the installation and configuration of Autoware on Ubuntu 22.04. Follow the [official documentation](https://autowarefoundation.github.io/autoware-documentation/main/installation/autoware/source-installation/) closely for further details.

>
>**Note:**
>This guide was last edited for the _Autoware_ version compatible with _ROS 2 Humble_ and _Ubuntu 22.04._.
>Review the [Edit History](README-bfh.md) for further information.
>

## About Autoware and ROS2
Autoware is an open-source software stack designed for autonomous driving, 
leveraging _ROS 2 Humble_, a middleware framework built for robotics applications.
 _ROS_ (_Robot Operating System_) is not an operating system in the traditional 
 sense but a collection of libraries and tools that facilitate the development of robotic applications,
  providing functionality like hardware abstraction, 
  device drivers, and communication between processes. 
  _ROS2_, the second generation of _ROS_, is designed to be more 
  robust, scalable, and suitable for real-time systems, making it ideal for complex systems like autonomous vehicles. _Autoware_ is specifically built with _ROS 2 Humble_, which is designed to run on _Ubuntu 22.04_, a widely used Linux distribution.

Because _ROS2_ depends heavily on the underlying operating system, any changes or inconsistencies in the base OS can lead to compatibility issues. This sensitivity to the OS is why _Autoware_ is tightly aligned with Ubuntu 22.04 to guarantee stability and predictable performance. _ROS 2 Humble_ provides essential tools like _DDS_ (_Data Distribution Service_) for communication and packages that enable sensor integration, mapping, localization, and path planning — all core features for autonomous vehicles. Autoware combines these capabilities into a cohesive stack tailored for self-driving tasks, ensuring a modular and flexible architecture for developers and researchers.

To mitigate any dependency problems and performance issues, you are strongly recommended to comply to these dependencies (_ROS 2 Humble_ and _Ubuntu 22.04_).

## Note on Dockerized installation vs. Source installation
Although _Autoware_ can be used dockerized, both for deployment and for developement, _AWSIM-Labs_ requires graphics card resources that are only accessible through a fine-grained setup. Creating a suitable container with a `NVIDIA Container Toolkit` installation, correctly maintained Hardware drivers, optimized ressource management, correct permission management, debugging and mitigated performance overhead is a challenging ordeal. Nontheless, a potential topic for further development in this project.

Therefor, _Source Installtion_ was the chosen option.

## Prerequisits

### 1. Ubuntu 22.04.
Since _Autoware_ is build with _ROS2_, and _ROS2_ (as well as _Autoware_) are developed on _Ubuntu 22.04_. Although there are possibilities to run _ROS2_ on _Windows 10_, it's generally not recommended, since dependency management is harder, package availability is limited, and performance issues can be problematic.

### 2. ROS2 Humble
As recommended in the [official installation guide](https://docs.ros.org/en/humble/Installation/Ubuntu-Install-Debs.html), intall the _ROS2_ _Humble_ distribution from `deb` packages.

#### 2.1 Set locale
#### 2.2 Setup Sources
#### 2.3 Install ROS2 Packages

#### 2.4 Sanity Check
When you successfully completed the steps above, try the _Talker-Listener_ example in order to verify that both the `C++` and `Python` APIs are working correctly.

#### 2.5 Troubleshooting
Watch out for any error output during the installation. These outputs often indicate missing `apt` packages that need to be installed prior to the _ROS2_ installation.
 
### 3. Prepare Git
It's very likely, that you already have `git` installed on you _Ubuntu_ installation. In this case, you can skip the next step (**3.1**).

#### 3.1 Install and Configure git
Open a terminal and enter the following command to install `git`:

```sh
sudo apt update
sudo apt install git 
```

Check the installation with the following command:

```sh
git --version
```
Follow the [_official git documentation_](https://git-scm.com/book/ms/v2/Getting-Started-First-Time-Git-Setup) to make some global git configurations

#### 3.2 Create an SSH Key and Register it to your Git account.
This step is recommended to simplify the VCS management when cloning _Autoware_ and creating an _Autoware_ workspace after initialization.

For _GitLab_ users:
Follow [this guide](https://docs.gitlab.com/ee/user/ssh.html)

For _GitHub_ users:
Follow [this guide](https://docs.github.com/en/authentication/connecting-to-github-with-ssh/adding-a-new-ssh-key-to-your-github-account)

## Autoware Source Installation
Please open up a browser window with the [official guide](https://autowarefoundation.github.io/autoware-documentation/main/installation/autoware/source-installation/#how-to-set-up-a-development-environment) in assistance to the notes.

There is an optional graphical user interface for building _Autoware_, this document will guide through the creation of a workspace with classical command line tools.

### 1. Setting up a development environment
In this step, all the dependencies used for Autoware will be installed. Some of them, like _ROS 2_, have been installed manually (for easier troubleshooting).

#### 1.1 Clone `autowarefoundation/autoware` repository
In your prefered location, clone `autowarefoundation/autoware` and move to the directory.
```sh
git clone https://github.com/autowarefoundation/autoware.git
cd autoware
```

#### 1.2 Run _Ansible_ script
Run the _Ansible_ script called `setup-dev-env.sh` to automatically isntall all missing dependencies.

```sh
./setup-dev-env.sh
```
Ansible will ask you for your sudo password, if you haven't provided it already.

The last lines of the output will look similar to this:

~~~
PLAY RECAP ******************************************************************************************
localhost                  : ok=151  changed=65   unreachable=0    failed=0    skipped=5    rescued=0    ignored=0   
~~~
As long as none of the _Ansible Playbook_'s tasks have failed, everything is fine.

If you run into any trouble, consult the troubleshooting page linked to in the official guide.

### 2 Set up a workspace
A workspace is a very central concept when developing with _ROS2_.
Workspaces leverage the modularization of distributed systems by allowing the projects source code to be split into multiple projects with their own workspaces. The source code of the workspaces is than connected via `overlay`, which is done by `sourcing` them. Exploring the concept of workspaces in more depth would be out of the scope of this project.

#### 2.1 Clone Repositories

Create a `src` directory and clone the repositories into it. The included workspaces are constructed by `vcstool`:

```sh
cd autoware
mkdir src
vcs import src < autoware.repos
```

#### 2.2 install dependend ROS packages
Directly from the guide:<br>
_"Autoware requires some_ ROS 2 _packages in addition to the core components. The tool `rosdep` allows an automatic search and installation of such dependencies."_

Source the _ROS 2_ distribution:
```sh
source /opt/ros/humble/setup.bash
```
Make sure all previously installed ROS packages are all up to date, before you move on:

```sh
sudo apt update && sudo apt upgrade
```
Update `rosdep` and install the missing _ROS2_ packages _Autoware_ requires:

```sh
rosdep update
rosdep install -y --from-paths src --ignore-src --rosdistro $ROS_DISTRO
```
#### 2.3 Optional: setup `cchache`:
In order to speed up consecutive builds, install and setup `cchache` with [this guide](https://autowarefoundation.github.io/autoware-documentation/main/how-to-guides/others/advanced-usage-of-colcon/#using-ccache-to-speed-up-recompilation).

#### 2.4 Build the workspace
Use the `colcon` command to build the workspace with the following command:
```sh
colcon build --symlink-install --cmake-args -DCMAKE_BUILD_TYPE=Release
```
For troubleshooting, visit the provided link in the official guide.

#### 2.5 Make Network configuration
Follow the [instructions for network configuration](https://autowarefoundation.github.io/autoware-documentation/main/installation/additional-settings-for-developers/network-configuration/) for a single computer setup.
Make sure to follow the instructions to make the settings permanent.

### 3 Update the Autoware workspace

Sometimes a workspace needs to be update, e.g. when the workspace needs to be updated to the state of a coworkers workspace, or fetch the newest
issues from a repository, described [here](https://autowarefoundation.github.io/autoware-documentation/main/installation/autoware/source-installation/#how-to-update-a-workspace)

Unfortunately, building can fail in some cases, this is why you can try to delete the `build`, `install`, and `log` folders:

```
rm -rf build install log
```

and then try building again:

```
colcon build --symlink-install --cmake-args -DCMAKE_BUILD_TYPE=Release
```

#### Fetching the new tags:
In order to fetch the tags from the current remote, you have to run the following command:

```
git fetch --tags
```