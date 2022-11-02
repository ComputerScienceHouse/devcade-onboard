# Devcade-onboard
The onboard menu and control software for the Devcade custom arcade system.


## Building

To make a build to run on the Idiot, do the following from `/onboard`:
```
dotnet publish -c Release -r linux-x64 --no-self-contained
```

To put it on the Idiot, compress the `publish` folder located at `\Devcade-onboard\onboard\bin\Release\netcoreapp3.1\linux-x64` and `scp` that to the Idiot.

## The Idiot

### Prereqs

Debian >=10

A user named `devcade`

`apt install xorg` and friends (I dont actually know what all is installed)

### Daemon

_daemons are always watching. They are always with you. So is Willard._

The Devcade Idiot is running Debian 10 with a very _very_ simple Xorg server setup. It has a systemd service configured to launch the onboarding program, along with said xorg server, as the `devcade` user.

You can find everything(tm) you need to set up the Devcade Idiot in `/idiot`. This includes the systemd service, which contains the path you need to install it at.

```
/etc/systemd/system/devcade-onboard.service
```

You'll also need to add/change some lines in your `/etc/X11/Xwrapper.config`

```
needs_root_rights=yes
allowed_users=anybody
```

This should be interactable as a normal systemd service, so `enable`/`disable` it as normal.

_Helpful Tip: Remember to `chmod +x onboard`. You may get weird syntax errors if you don't_

### Virtual Machine

There's a virtual machine available that can allow you to emulate a Linux box similar to the Idiot. This can be useful for testing anything weird you're doing, or just working on the idiot's systems itself.

The VM consists of a Debian 10 installation that attempts to mirror packages installed on the Idiot. It has a Xserver, and Guest Additions installed, so that you can map your project into the machine.

First, download the [.ova file](https://members.csh.rit.edu/~wilnil/devcade-debian.ova). Import it into [VirtualBox](https://www.virtualbox.org/wiki/Downloads).

<h3>Setting Up the Shared Folder</h3>
<ol>
<li>Select the guest machine you wish to share files with</li>
<li>Click <em>Settings &gt; Shared Folders</em></li>
<li>Right-click and select <em>Add Shared Folder </em>and use the following settings:<br>
<blockquote><p><strong>Folder Path:</strong> <em>Click the dropdown arrow, select <strong>Other</strong>, and navigate to the folder you would like to share</em><br>
<strong>Folder Name:</strong> <em>Anything to identify it on the guest machine</em><br>
<strong>Read-Only:</strong> Unchecked <em>(Checked, if you are exclusively pulling files <strong>from the host</strong>)<br>
<strong>Auto-Mount:</strong> Checked<br>
<strong>Mount Point:</strong> <em>/home/Code/devcade/publish</em> </em></p></blockquote>
</li>
<li>Click <em>OK</em></li>
</ol>

