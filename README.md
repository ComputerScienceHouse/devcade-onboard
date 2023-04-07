# Devcade-onboard
The onboard menu and control software for the Devcade custom arcade system.

## Building on a devcade system

Run the `update_onboard.sh` script located in install

## Building (manual)

To build and run on the DCU, do the following from `/onboard`:
```
dotnet publish -c Release -r linux-x64 --no-self-contained
```

To put it on the DCU (devcade compute unit), compress the `publish` folder located at `\Devcade-onboard\onboard\bin\Release\netcoreapp3.1\linux-x64` and `scp` that to the DCU.

## The Devcade Compute Unit (DCU)

### Prereqs

Debian >=10

A user named `devcade`

`apt install xinit xterm git build-essential wget openbox compton pulseaudio x11-xserver-utils`

Also install dotnet-sdk-6.0 which requires adding microsofts package repo.

### Daemon

_daemons are always watching. They are always with you. So is Willard._

The DCU is running Debian 10 with a very _very_ simple Xorg server setup. It has [xlogin](https://github.com/joukewitteveen/xlogin) configured to launch the onboarding program, along with said xorg server, as the `devcade` user.

You can find everything(tm) you need to set up the DCU in `/install`. This repo has a submodule, `xlogin` that can be cloned down with `git submodule update --init --recursive`.

1. Run the `update_onboard.sh` script in `install/`

2. `cp install/.xinitrc /home/devcade/`

2. `mkdir /home/devcade/.config/openbox && cp install/rc.xml /home/devcade/.config/openbox/rc.xml`

3. To install `xlogin`, do the following

```
cd install/xlogin
sudo make install
sudo systemctl enable --now xlogin@devcade
```

_Helpful Tip: Remember to `chmod +x onboard`. You may get weird syntax errors if you don't_

## Setting up a dev environment

To setup and launch a development environment, you can do the following:

### Env Vars
Create a `.env` file with the following values.

```
AWS_ACCESS_KEY_ID
AWS_SECRET_ACCESS_KEY
AWS_DEFAULT_REGION
DEVCADE_API_DOMAIN
```

### Building and Launching the Container

```
cd dev-environment
./build-environment.sh
./launch-environment.sh
```

#### `mgcb`

The container has `mgcb-editor` installed. To run that, do this:
`dotnet mgcb-editor`
