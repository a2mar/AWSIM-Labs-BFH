# Autoware Installation

This guide provides a streamlined overview to assist in the installation and configuration of Autoware on Ubuntu 22.04. Follow the [official documentation](https://autowarefoundation.github.io/autoware-documentation/main/installation/autoware/source-installation/) closely for further details.

## About Autoware and ROS2
Autoware is an open-source software stack designed for autonomous driving, leveraging _ROS 2 Humble_, a middleware framework built for robotics applications. _ROS_ (_Robot Operating System_) is not an operating system in the traditional sense but a collection of libraries and tools that facilitate the development of robotic applications, providing functionality like hardware abstraction, device drivers, and communication between processes. _ROS2_, the second generation of _ROS_, is designed to be more robust, scalable, and suitable for real-time systems, making it ideal for complex systems like autonomous vehicles. _Autoware_ is specifically built with _ROS 2 Humble_, which is designed to run on _Ubuntu 22.04_, a widely used Linux distribution.

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
