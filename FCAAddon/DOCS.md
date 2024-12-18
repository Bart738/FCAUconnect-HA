![Uconnect Logo](https://www.driveuconnect.com/content/dam/uconnect/global/header/Uconnect-small.png)

source, credit and most code from https://github.com/wubbl0rz/FiatChamp

Connect your FCA/Stellantis car 🚗 or truck 🚚 to Home Assistant. Needs a vehicle with valid subscription tied to Uconnect account.

## Car Brands

- Jeep: Works ✅
- Fiat: Unknown ⛔
- Ram: Unknown ⛔
- Dodge: Unknown ⛔
- AlfaRomeo: Unknown ⛔


## Tested Vehicles

- 2024 Jeep Wagoneer L

## Prerequisites 📃

- Official Home Assistant MQTT Addon (recommended) running or external mqtt broker. Broker must be connected to Home Assistant MQTT integration.

![image](https://user-images.githubusercontent.com/30373916/196045271-44287d3f-93ba-49c0-a72f-0bc92042efbb.png)

Make sure your car works with one of the following Uconnect sites. Older vehicles that only use mopar.com do not seem to work.

- Fiat: https://myuconnect.fiat.com/
- Jeep: https://myuconnect.jeep.com
- Ram: https://connect.ramtrucks.com/
- Dodge: https://connect.dodge.com
- AlfaRomeo: https://myalfaconnect.alfaromeo.com/

## Features ✔️

- Imports values like battery level 🔋, tire pressure ‍💨, odometer ⏲ etc. into Home Assistant.
- Multiple Brands: Fiat, Jeep, Ram, Dodge, AlfaRomeo
- Supports multiple cars on the same account. 🚙🚗🚕
- Location tracking.🌍
- Home Assistant zones (home 🏠, work 🏦 etc.) support.
- Uses the same data source as the official app 📱.
- Remote commands (open doors 🚪, switch air conditioner 🧊 on , ...) Some commands may not work with all cars. Available commands are:
  - "UpdateLocation" (updates gps location of the car) 
  - "RefreshBatteryStatus" (refresh battery level %)
  - "DeepRefresh" (same as "RefreshBatteryStatus")
  - "Blink" (blink lights)
  - "ChargeNOW" (starts charging)
  - "Trunk" (open/close trunk lock)
  - "DoorLock" (unlock/lock vehicle doors)
  - "HVAC" (turn on the temperature preconditioning in the car)
  - "StartEngine" (remote starts the vehicle engine)
  - "StopEngine" (cancels the remote start request)
  - "SuppressAlarm" (suppresses the vehicle theft alarm)

## What doesn't work (yet)? ❌

- Eco Reports (statistics). I could not find any API yet. The official app shows it so in theory it should be accessible.
- Live Vehicle Status. Some users report that you can find the engine/lock status but I haven't been able to replicate this on my 2024 Wagoneer L.

## What will NEVER work? ❌

- Things the FCA api does not support. Like real time tracking or adjusting the music volume.


## Options / Usage

At startup the Addon will automatically connect to your Home Assistant MQTT Broker. You can configure your own MQTT server in the configuration.

- PIN is only needed if you want to send commands to your car. Its the same PIN used by the official app or website.
- Use DEBUG carefully. It will dump many informations to the log including session tokens and sensitive informations.
- Automatic refresh of location and battery level may drain your battery a bit more. The car have to wakeup some parts, read new values and sent them back. This will get executed every "Refresh interval" and at every command even if your car is not at home. __Recommendation:__  Use a Home Assistant automation instead. I have setup an automation that if the odometer has gone up the car will update it's location/battery status.
- Mqtt override can be used if you want to utilize an external mqtt broker. __You do not need this if you are using the official Home Assistant mqtt addon.__

<img src="https://raw.githubusercontent.com/Blueion76/FCAUconnect-HA/refs/heads/master/options.png" width="700px">
