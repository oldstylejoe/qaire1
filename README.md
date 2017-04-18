A questionnaire app intended for use on a MS surface (with a pen). Timing of input is recorded at high precision for compatibility with EEG.

Data are saved to a Documents/qaire1 folder (must exist!).

Installation:
1. Download the source from GitHub and unzip to desired location.

2. Download VisualStudio (2017) from: www.visualstudio.com/downloads/-You'll need to install the Universal Windows Platform development and .NET desktop development (for the .NET framework)

3. After installation and computer restart, open qaire1.sln in VisualStudio

4. Edit the file "questions.xml" to set up questionnaire; make sure all referenced files (e.g. image files) are in the qaire1\Assets folder

5. Go to the following in VisualStudio: Project -> Store -> Create App Packages

6. In the "Create Your Packages" dialog window, choose "No" for "Do you want to build packages to upload to Windows Store", and hit "Next"

7. Choose the desired output location and Architecture (x64 suffices for Surfacetablets), and hit "Create"

8. If the .xml file was edited without errors, the build process should finish after a few minutes.

9. Go to the output folder and look for the script called Add-AppDevPackage. Run the script with powershell (right click on the file for that option).

10. The app should now be installed. Run the program "qaire1" from the startup menu to use the questionnaire.

11. To uninstall the app easily, search for the app in the "Ask me anything" bar, and right-click on the app. There should be an option to uninstall the program.
