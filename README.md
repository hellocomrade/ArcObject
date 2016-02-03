# ArcObject .Net with VS 2013 #

Please take this repository as a memo to walk you through the AO setup, debug and development...

If you can't do, you teach :)


## Prerequisites: Installation##
## Lesson 1: Setup Environment and Debug ##
OK, let's start up our first AO project in VS2013! On menu bar: FILE -> New Project, then open the tree structure inside the left pane on the opening dialog, find Visual C# -> ArcGIS -> Desktop Add-ins -> ArcMap Add-in.

In the ArGIS Add-Ins Wizard, make sure to check "Button" option under Add-in Types. This will create a template of a button control for ArcMap.
You should be able to find a cs file with name "Button1.cs". Please replace this file with the on at [here](https://github.com/hellocomrade/arcobject/blob/master/lesson1/Button1.cs).
Before we build the button control, open Soultion Explorer of your VS 2013, right-click on the project name and choose properties. Double Check 2 places:

1. Target framework under Application section should be ".NET Framework 4";
2. Under Debug section, make sure the "Start external program" is set as your ArcMap program;

Still under Solution Explorer, right click on the References and choose "Add ArcGIS References...". We need the following two references in order to build this solution. They are not included by this default template:

1. ESRI.ArcGIS.Geometry
2. ESRI.ArcGIS.Carto

Now you are ready to build the solution! Before you do that, there is still some housekeeping we have to do on ArcMap end. Go to your ArcMap installation folder, usually it is located at: C:\Program Files (x86)\ArcGIS\Desktop10.3\bin, find a file named "ArcMap.exe.config". Open it up with administrator privilege. It is an XML file and search the following section located at the top of the file:
```xml
<startup>
    <!--<supportedRuntime version="v4.0.30319"/>-->
    <supportedRuntime version="v2.0.50727"/>
</startup>
```
By default, ArcMap is configured to run against .Net Framework Runtime 2.0, which is a conflict with the button control we are going to build in VS 2013. Comment out that line and uncomment the line above to enable .Net Framework Runtime 4.0 for ArcMap. OK, now you should be able to build it without any issue. In order to make sure we are on the same page for the following section, please check your "config.esriaddinx" file. You can find this file under Solution Explorer. Here is what I have (I did remove unrelated sections)
```xml
<ESRI.Configuration xmlns="http://schemas.esri.com/Desktop/AddIns" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <Name>ArcMapAddin1</Name>
  ......
  <Targets>
    <Target name="Desktop" version="10.3" />
  </Targets>
  <AddIn language="CLR4.0" library="ArcMapAddin1.dll" namespace="ArcMapAddin1">
    <ArcMap>
      <Commands>
        <Button id="ArcMapAddin1_Button1" class="Button1" message="Add-in command generated by Visual Studio project wizard." caption="My Button" tip="Add-in command tooltip." category="Add-In Controls" image="Images\Button1.png" />
      </Commands>
    </ArcMap>
  </AddIn>
</ESRI.Configuration>
```
It gives us a good summary on the tool we just built. Its name is "ArcMapAddin1" and its target is against ArcMap 10.3 running against .Net Framework Common Lauguage Runtime 4.0. This button control was given the caption as "My Button" and categorized under "Add-In Controls". Keep these in mind, you will need them later.

If we click the "Start" button with green arrow in VS2013, you should be able to see ArcMap is starting. VS2013 will do all the dirty jobs for us: invoke ArcMap 10.3 and attach the debugger to its process, load all necessary symbols from various assemblies to  facilitate debug...We are then ready to debug!

You can put a breakpoint on any line of source in Button1.cs, but nothing happens, right? It's because this control is a UI component and requires the user to click on it to invoke any action. So, where is our button? It is not on the UI! Well, it is really inconvenient and ESRI can do a better job by automatically adding the button onto the toolbar during debug. 

Anyway, we will have to do this manaully. You will need to go to Customize -> Add-In Manager, you should be able to see our button under "My Add-Ins", thanks to ESRI! The name should match the name in config.esriaddinx, the XML file that I ask you to pay attention.

Then, click on the button "Customize...", on Toolbars tab, you can create a new toolbar to hold our work. I created one called "Monkeybar", you can name it whatever, just make sure it won't duplicate the existing ones in ArcMap. Switch to Commands tab, the left pane lists all available categories, remember what I told you to remember? The category name is "Add-In Controls"! If you click on it, on the right side, you should be able to see "My Button" as the command. 

Now you can drag "My Button" onto your new toolbar! Next step will be adding some layers onto the map. The quickest way to do it is by adding a basemap which will bring in the coordinate system as well. I chose ESRI World Topo. It has a web Mercator type of projection. You can find my mxd file  [here](https://github.com/hellocomrade/arcobject/blob/master/lesson1/lesson1.mxd) 

Now if you click the button (By default, it is with a blue round icon), you should be able to see four red round dots showing on the four corners of the map. You may need to zoom to the full extent to see them. If you put a break point at line [59](https://github.com/hellocomrade/ArcObject/blob/master/lesson1/Button1.cs#L59) of Button1.cs and click our button in ArcMap, the execution will be suspended and you can do step by step debug on Button1.cs source inside VS2013.

You may notice that most of logics of Button1.cs are outside OnClick function. What if you want to debug the code, say, at line [33](https://github.com/hellocomrade/ArcObject/blob/master/lesson1/Button1.cs#L33)? Well, that function is called "the default contructor of the class" and only invoked once when this button is clicked the every first time. In order to debug the codes in there, we will have to termintate current debug session by clicking the red square button in VS 2013, then put a new breakpoint at line [33](https://github.com/hellocomrade/ArcObject/blob/master/lesson1/Button1.cs#L33) and start over again.

BTW, can anyone tell me what the line [36](https://github.com/hellocomrade/ArcObject/blob/master/lesson1/Button1.cs#L36) means? What is the section on the right side of += sign for? If you don't know, it is time to spend some time on C# before you move on future lessons.




