# Ragnarok Rebuild (TCP/IP Websocket Ver.)

Server and client for a Ragnarok Online-like game. Some assembly required.

**Note**: This repository contains no game assets from Ragnarok Online. You will need to provide those yourself.

**Warning**: The code is horrifying, would not recommend reading.

## Requirements

- Unity 2022.3.6f1 or higher
- .NET 7
- Lack of sanity

## Setting things up

- Load up the server csproj project, and make sure the server is able to build successfully. You don't need to run it yet.
- In the root project directory, run "updateclient.bat". This copies server definitions and data over to the client.
- Open the client directory in Unity.
- From the Ragnarok menu, select "Copy data from client data folder".
- First you will be prompted to set a path to the extracted data.grf files from an original client. For this import process to work correctly, the files will need to have been extracted with the right locale and have working korean file names.
- You then will receive a warning on how long the process will take. In testing this took about 2 hours to complete. If you want to process fewer maps, you can import them one at a time via the 'Ragnarok -> Import Maps' option, or you can trim the files RoRebuildServer/ServerData/Db/Maps.csv and Instances.csv to only have a small subset of maps, and then rerun the updateclient.bat.
- Once all the maps are imported, open the Lighting Manager window (Ragnarok -> Open Lighting Manager).
- Place all the scenes in the Scenes\Maps folder into the list of scenes on the Lighting Manager window. Then, click 'bake all scenes' (not bake all). This can take several hours depending on your GPU. You can skip this step but maps will not display with any lighting until you do.
- Once lighting is baked, you can select the option 'Make Minimaps' to generate minimap images.
- Copy any of your BGM over from your ragnarok client into the Music folder.
- Finally, select 'Ragnarok -> Update Addressables' to link all the newly imported assets. Any time you add sprites or maps, you will need to do this again.
- At this point, you should be able to run the server. Make sure you set visual studio to run the server as a standalone rather than via IIS.
- Once the server is running, hitting play in the editor should allow you to connect.
- You might get errors related to missing reference on GameDataManager on your first run, to fix this open the scene MainScene in the Scenes folder and make sure the data fields correctly link to the files in Assets/Data.
