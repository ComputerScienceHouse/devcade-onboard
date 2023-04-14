# Devcade-onboard
The onboard menu and control software for the Devcade custom arcade system.

## Building

Run the `update_onboard.sh` script located in HACKING

## Building (manual)

To build and run on the DCU, do the following from `./onboard/frontend`:
```
dotnet publish -c Release -r linux-x64 --no-self-contained
```
And the following fron `./onboard/backend`:
```
cargo build --release --target x86_64-unknown-linux-gnu
```
Note: The backend requires the nightly compiler to build, as it uses features that have not been merged into stable rustc

To put it on the DCU, compress the `publish` folder located at `./onboard/frontend/bin/Release/netcoreapp3.1/linux-x64` and `scp` that to the DCU.
You'll also want to `scp` `./onboard/backend/target/release` to the DCU. 

## The DCU

### Prereqs

Debian >=10

A user named `devcade`

`apt install xterm openbox compton` and friends (I dont actually know what all is installed)

### Daemon

_daemons are always watching. They are always with you. So is Willard._

The Devcade DCU is running Debian 10 with a very _very_ simple Xorg server setup. It has [xlogin](https://github.com/joukewitteveen/xlogin) configured to launch the onboarding program, along with said xorg server, as the `devcade` user.

You can find everything(tm) you need to set up the Devcade DCU in `/dcu`. This repo has a submodule, `xlogin` that can be cloned down with `git submodule update --init --recursive`.

1. Run the `update_onboard.sh` script in `HACKING/`

2. `cp dcu/.xinitrc /home/devcade/`

2. `mkdir /home/devcade/.config/openbox && cp dcu/rc.xml /home/devcade/.config/openbox/rc.xml`

3. To install `xlogin`, do the following

```
cd dcu/xlogin
sudo make install
sudo systemctl enable --now xlogin@devcade
```

_Helpful Tip: Remember to `chmod +x onboard`. You may get weird syntax errors if you don't_

## HACKING

To setup and launch a development environment, you can do the following:

### Env Vars

There is a file called .env.template in the `./onboard` folder. Fill this in with appropriate values for the backend and frontend.

### Building and Launching the Container

```
cd HACKING
./build-environment.sh
./launch-environment.sh
```

#### `mgcb`

The container has `mgcb-editor` installed. To run that, do this:
`dotnet mgcb-editor`
