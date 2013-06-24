/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the Apache License, Version 2.0, please send an email to 
 * vspython@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 * ***************************************************************************/


using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using EnvDTE;
using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using IServiceProvider = System.IServiceProvider;

namespace ProjectSystem.Infrastructure
{
    public static class Utilities
    {
        private const string _reservedName = "(\\b(nul|con|aux|prn)\\b)|(\\b((com|lpt)[0-9])\\b)";
        private const string _invalidChars = "([\\/:*?\"<>|#%])";
        private const string _regexToUseForFileName = _reservedName + "|" + _invalidChars;
        private static readonly char[] curlyBraces = new[] {'{', '}'};
        private static readonly Regex _unsafeFileNameCharactersRegex = new Regex(_regexToUseForFileName, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static readonly Regex _unsafeCharactersRegex = new Regex(_invalidChars, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>
        ///     Look in the registry under the current hive for the path
        ///     of MSBuild
        /// </summary>
        /// <returns></returns>
        /// <summary>
        ///     Is Visual Studio in design mode.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <returns>true if visual studio is in design mode</returns>
        public static bool IsVisualStudioInDesignMode(IServiceProvider site)
        {
            ArgumentNotNull("site", site);

            var selectionMonitor = site.GetService(typeof (IVsMonitorSelection)) as IVsMonitorSelection;
            uint cookie = 0;
            int active = 0;
            Guid designContext = VSConstants.UICONTEXT_DesignMode;
            ErrorHandler.ThrowOnFailure(selectionMonitor.GetCmdUIContextCookie(ref designContext, out cookie));
            ErrorHandler.ThrowOnFailure(selectionMonitor.IsCmdUIContextActive(cookie, out active));
            return active != 0;
        }

        /// <include file='doc\VsShellUtilities.uex' path='docs/doc[@for="Utilities.IsInAutomationFunction"]/*' />
        /// <devdoc>
        ///     Is an extensibility object executing an automation function.
        /// </devdoc>
        /// <param name="serviceProvider">The service provider.</param>
        /// <returns>true if the extensiblity object is executing an automation function.</returns>
        public static bool IsInAutomationFunction(IServiceProvider serviceProvider)
        {
            ArgumentNotNull("serviceProvider", serviceProvider);

            var extensibility = serviceProvider.GetService(typeof (IVsExtensibility)) as IVsExtensibility3;

            if (extensibility == null)
            {
                throw new InvalidOperationException();
            }
            int inAutomation = 0;
            ErrorHandler.ThrowOnFailure(extensibility.IsInAutomationFunction(out inAutomation));
            return inAutomation != 0;
        }

        /// <summary>
        ///     Creates a semicolon delinited list of strings. This can be used to provide the properties for VSHPROPID_CfgPropertyPagesCLSIDList, VSHPROPID_PropertyPagesCLSIDList, VSHPROPID_PriorityPropertyPagesCLSIDList
        /// </summary>
        /// <param name="guids">An array of Guids.</param>
        /// <returns>A semicolon delimited string, or null</returns>
        public static string CreateSemicolonDelimitedListOfStringFromGuids(Guid[] guids)
        {
            if (guids == null || guids.Length == 0)
            {
                return "";
            }

            // Create a StringBuilder with a pre-allocated buffer big enough for the
            // final string. 39 is the length of a GUID in the "B" form plus the final ';'
            var stringList = new StringBuilder(39*guids.Length);
            for (int i = 0; i < guids.Length; i++)
            {
                stringList.Append(guids[i].ToString("B"));
                stringList.Append(";");
            }

            return stringList.ToString().TrimEnd(';');
        }

        /// <summary>
        ///     Take list of guids as a single string and generate an array of Guids from it
        /// </summary>
        /// <param name="guidList">Semi-colon separated list of Guids</param>
        /// <returns>Array of Guids</returns>
        public static Guid[] GuidsArrayFromSemicolonDelimitedStringOfGuids(string guidList)
        {
            if (guidList == null)
            {
                return null;
            }

            var guids = new List<Guid>();
            string[] guidsStrings = guidList.Split(';');
            foreach (string guid in guidsStrings)
            {
                if (!String.IsNullOrEmpty(guid))
                    guids.Add(new Guid(guid.Trim(curlyBraces)));
            }

            return guids.ToArray();
        }

        internal static void CheckNotNull(object value)
        {
            if (value == null)
            {
                throw new InvalidOperationException();
            }
        }

        internal static void ArgumentNotNull(string name, object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(name);
            }
        }

        internal static void ArgumentNotNullOrEmpty(string name, string value)
        {
            if (String.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException(name);
            }
        }


        /// <summary>
        ///     Creates a CALPOLESTR from a list of strings
        ///     It is the responsability of the caller to release this memory.
        /// </summary>
        /// <param name="guids"></param>
        /// <returns>A CALPOLESTR that was created from the the list of strings.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "CALPOLESTR")]
        public static CALPOLESTR CreateCALPOLESTR(IList<string> strings)
        {
            var calpolStr = new CALPOLESTR();

            if (strings != null)
            {
                // Demand unmanaged permissions in order to access unmanaged memory.
                new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();

                calpolStr.cElems = (uint) strings.Count;

                int size = Marshal.SizeOf(typeof (IntPtr));

                calpolStr.pElems = Marshal.AllocCoTaskMem(strings.Count*size);

                IntPtr ptr = calpolStr.pElems;

                foreach (string aString in strings)
                {
                    IntPtr tempPtr = Marshal.StringToCoTaskMemUni(aString);
                    Marshal.WriteIntPtr(ptr, tempPtr);
                    ptr = new IntPtr(ptr.ToInt64() + size);
                }
            }

            return calpolStr;
        }

        /// <summary>
        ///     Creates a CADWORD from a list of tagVsSccFilesFlags. Memory is allocated for the elems.
        ///     It is the responsability of the caller to release this memory.
        /// </summary>
        /// <param name="guids"></param>
        /// <returns>A CADWORD created from the list of tagVsSccFilesFlags.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "CADWORD")]
        public static CADWORD CreateCADWORD(IList<tagVsSccFilesFlags> flags)
        {
            var cadWord = new CADWORD();

            if (flags != null)
            {
                // Demand unmanaged permissions in order to access unmanaged memory.
                new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();

                cadWord.cElems = (uint) flags.Count;

                int size = Marshal.SizeOf(typeof (UInt32));

                cadWord.pElems = Marshal.AllocCoTaskMem(flags.Count*size);

                IntPtr ptr = cadWord.pElems;

                foreach (tagVsSccFilesFlags flag in flags)
                {
                    Marshal.WriteInt32(ptr, (int) flag);
                    ptr = new IntPtr(ptr.ToInt64() + size);
                }
            }

            return cadWord;
        }

        /// <summary>
        ///     Splits a bitmap from a Stream into an ImageList
        /// </summary>
        /// <param name="imageStream">A Stream representing a Bitmap</param>
        /// <returns>An ImageList object representing the images from the given stream</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public static ImageList GetImageList(Stream imageStream)
        {
            var ilist = new ImageList();

            if (imageStream == null)
            {
                return ilist;
            }
            ilist.ColorDepth = ColorDepth.Depth24Bit;
            ilist.ImageSize = new Size(16, 16);
            var bitmap = new Bitmap(imageStream);
            ilist.Images.AddStrip(bitmap);
            ilist.TransparentColor = Color.Magenta;
            return ilist;
        }

        /// <summary>
        ///     Gets the active configuration name.
        /// </summary>
        /// <param name="automationObject">The automation object.</param>
        /// <returns>The name of the active configuartion.</returns>
        internal static string GetActiveConfigurationName(EnvDTE.Project automationObject)
        {
            ArgumentNotNull("automationObject", automationObject);

            string currentConfigName = string.Empty;
            if (automationObject.ConfigurationManager != null)
            {
                Configuration activeConfig = automationObject.ConfigurationManager.ActiveConfiguration;
                if (activeConfig != null)
                {
                    currentConfigName = activeConfig.ConfigurationName;
                }
            }
            return currentConfigName;
        }


        /// <summary>
        ///     Verifies that two objects represent the same instance of a COM object.
        ///     This essentially compares the IUnkown pointers of the 2 objects.
        ///     This is needed in scenario where aggregation is involved.
        /// </summary>
        /// <param name="obj1">Can be an object, interface or IntPtr</param>
        /// <param name="obj2">Can be an object, interface or IntPtr</param>
        /// <returns>True if the 2 items represent the same thing</returns>
        [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "obj")]
        public static bool IsSameComObject(object obj1, object obj2)
        {
            bool isSame = false;
            IntPtr unknown1 = IntPtr.Zero;
            IntPtr unknown2 = IntPtr.Zero;
            try
            {
                // If we have 2 null, then they are not COM objects and as such "it's not the same COM object"
                if (obj1 != null && obj2 != null)
                {
                    unknown1 = QueryInterfaceIUnknown(obj1);
                    unknown2 = QueryInterfaceIUnknown(obj2);

                    isSame = Equals(unknown1, unknown2);
                }
            }
            finally
            {
                if (unknown1 != IntPtr.Zero)
                {
                    Marshal.Release(unknown1);
                }

                if (unknown2 != IntPtr.Zero)
                {
                    Marshal.Release(unknown2);
                }
            }

            return isSame;
        }

        /// <summary>
        ///     Retrieve the IUnknown for the managed or COM object passed in.
        /// </summary>
        /// <param name="objToQuery">Managed or COM object.</param>
        /// <returns>Pointer to the IUnknown interface of the object.</returns>
        internal static IntPtr QueryInterfaceIUnknown(object objToQuery)
        {
            bool releaseIt = false;
            IntPtr unknown = IntPtr.Zero;
            IntPtr result;
            try
            {
                if (objToQuery is IntPtr)
                {
                    unknown = (IntPtr) objToQuery;
                }
                else
                {
                    // This is a managed object (or RCW)
                    unknown = Marshal.GetIUnknownForObject(objToQuery);
                    releaseIt = true;
                }

                // We might already have an IUnknown, but if this is an aggregated
                // object, it may not be THE IUnknown until we QI for it.				
                Guid IID_IUnknown = VSConstants.IID_IUnknown;
                ErrorHandler.ThrowOnFailure(Marshal.QueryInterface(unknown, ref IID_IUnknown, out result));
            }
            finally
            {
                if (releaseIt && unknown != IntPtr.Zero)
                {
                    Marshal.Release(unknown);
                }
            }

            return result;
        }

        /// <summary>
        ///     Returns true if thename that can represent a path, absolut or relative, or a file name contains invalid filename characters.
        /// </summary>
        /// <param name="name">File name</param>
        /// <returns>true if file name is invalid</returns>
        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0",
            Justification = "The name is validated.")]
        public static bool ContainsInvalidFileNameChars(string name)
        {
            if (String.IsNullOrEmpty(name))
            {
                return true;
            }

            try
            {
                if (Path.IsPathRooted(name) && !name.StartsWith(@"\\", StringComparison.Ordinal))
                {
                    string root = Path.GetPathRoot(name);
                    name = name.Substring(root.Length);
                }
            }
                // The Path methods used by ContainsInvalidFileNameChars return argument exception if the filePath contains invalid characters.
            catch (ArgumentException)
            {
                return true;
            }

            var uri = new Url(name);

            // This might be confusing bur Url.IsFile means that the uri represented by the name is either absolut or relative.
            if (uri.IsFile)
            {
                string[] segments = uri.Segments;
                if (segments != null && segments.Length > 0)
                {
                    foreach (string segment in segments)
                    {
                        if (IsFilePartInValid(segment))
                        {
                            return true;
                        }
                    }

                    // Now the last segment should be specially taken care, since that cannot be all dots or spaces.
                    string lastSegment = segments[segments.Length - 1];
                    string filePart = Path.GetFileNameWithoutExtension(lastSegment);
                    // if the file is only an extension (.foo) then it's ok, otherwise we need to do the special checks.
                    if (filePart.Length != 0 && (IsFileNameAllGivenCharacter('.', filePart) || IsFileNameAllGivenCharacter(' ', filePart)))
                    {
                        return true;
                    }
                }
            }
            else
            {
                // The assumption here is that we got a file name.
                string filePart = Path.GetFileNameWithoutExtension(name);
                if (IsFileNameAllGivenCharacter('.', filePart) || IsFileNameAllGivenCharacter(' ', filePart))
                {
                    return true;
                }


                return IsFilePartInValid(name);
            }

            return false;
        }

        /// Cehcks if a file name is valid.
        /// </devdoc>
        /// <param name="fileName">The name of the file</param>
        /// <returns>True if the file is valid.</returns>
        public static bool IsFileNameInvalid(string fileName)
        {
            if (String.IsNullOrEmpty(fileName))
            {
                return true;
            }

            if (IsFileNameAllGivenCharacter('.', fileName) || IsFileNameAllGivenCharacter(' ', fileName))
            {
                return true;
            }


            return IsFilePartInValid(fileName);
        }

        /// <summary>
        ///     Initializes the in memory project. Sets BuildEnabled on the project to true.
        /// </summary>
        /// <param name="engine">The build engine to use to create a build project.</param>
        /// <param name="fullProjectPath">The full path of the project.</param>
        /// <returns>A loaded msbuild project.</returns>
        internal static Microsoft.Build.Evaluation.Project InitializeMsBuildProject(ProjectCollection buildEngine, string fullProjectPath)
        {
            ArgumentNotNullOrEmpty("fullProjectPath", fullProjectPath);

            // Call GetFullPath to expand any relative path passed into this method.
            fullProjectPath = CommonUtils.NormalizePath(fullProjectPath);


            // Check if the project already has been loaded with the fullProjectPath. If yes return the build project associated to it.
            var loadedProject = new List<Microsoft.Build.Evaluation.Project>(buildEngine.GetLoadedProjects(fullProjectPath));
            Microsoft.Build.Evaluation.Project buildProject = loadedProject != null && loadedProject.Count > 0 && loadedProject[0] != null ? loadedProject[0] : null;

            if (buildProject == null)
            {
                buildProject = buildEngine.LoadProject(fullProjectPath);
            }

            return buildProject;
        }

        /// <summary>
        ///     Loads a project file for the file. If the build project exists and it was loaded with a different file then it is unloaded first.
        /// </summary>
        /// <param name="engine">The build engine to use to create a build project.</param>
        /// <param name="fullProjectPath">The full path of the project.</param>
        /// <param name="exitingBuildProject">An Existing build project that will be reloaded.</param>
        /// <returns>A loaded msbuild project.</returns>
        internal static Microsoft.Build.Evaluation.Project ReinitializeMsBuildProject(ProjectCollection buildEngine, string fullProjectPath, Microsoft.Build.Evaluation.Project exitingBuildProject)
        {
            // If we have a build project that has been loaded with another file unload it.
            try
            {
                if (exitingBuildProject != null && exitingBuildProject.ProjectCollection != null && !CommonUtils.IsSamePath(exitingBuildProject.FullPath, fullProjectPath))
                {
                    buildEngine.UnloadProject(exitingBuildProject);
                }
            }
                // We  catch Invalid operation exception because if the project was unloaded while we touch the ParentEngine the msbuild API throws. 
                // Is there a way to figure out that a project was unloaded?
            catch (InvalidOperationException)
            {
            }

            return InitializeMsBuildProject(buildEngine, fullProjectPath);
        }

        /// <summary>
        ///     Initialize the build engine. Sets the build enabled property to true. The engine is initialzed if the passed in engine is null or does not have its bin path set.
        /// </summary>
        /// <param name="engine">An instance of MSBuild.ProjectCollection build engine, that will be checked if initialized.</param>
        /// <param name="engine">The service provider.</param>
        /// <returns>The buildengine to use.</returns>
        internal static ProjectCollection InitializeMsBuildEngine(ProjectCollection existingEngine, IServiceProvider serviceProvider)
        {
            ArgumentNotNull("serviceProvider", serviceProvider);

            if (existingEngine == null)
            {
                ProjectCollection buildEngine = ProjectCollection.GlobalProjectCollection;
                return buildEngine;
            }

            return existingEngine;
        }

        /// <summary>
        ///     >
        ///     Checks if the file name is all the given character.
        /// </summary>
        private static bool IsFileNameAllGivenCharacter(char c, string fileName)
        {
            // A valid file name cannot be all "c" .
            int charFound = 0;
            for (charFound = 0; charFound < fileName.Length && fileName[charFound] == c; ++charFound) ;
            if (charFound >= fileName.Length)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Checks whether a file part contains valid characters. The file part can be any part of a non rooted path.
        /// </summary>
        /// <param name="filePart"></param>
        /// <returns></returns>
        private static bool IsFilePartInValid(string filePart)
        {
            if (String.IsNullOrEmpty(filePart))
            {
                return true;
            }
            String fileNameToVerify = filePart;

            // Define a regular expression that covers all characters that are not in the safe character sets.
            // It is compiled for performance.

            // The filePart might still be a file and extension. If it is like that then we must check them separately, since different rules apply
            string extension = String.Empty;
            try
            {
                extension = Path.GetExtension(filePart);
            }
                // We catch the ArgumentException because we want this method to return true if the filename is not valid. FilePart could be for example #¤&%"¤&"% and that would throw ArgumentException on GetExtension
            catch (ArgumentException)
            {
                return true;
            }

            if (!String.IsNullOrEmpty(extension))
            {
                // Check the extension first
                bool isMatch = _unsafeCharactersRegex.IsMatch(extension);
                if (isMatch)
                {
                    return isMatch;
                }

                // We want to verify here everything but the extension.
                // We cannot use GetFileNameWithoutExtension because it might be that for example (..\\filename.txt) is passed in and that should fail, since that is not a valid filename.
                fileNameToVerify = filePart.Substring(0, filePart.Length - extension.Length);

                if (String.IsNullOrEmpty(fileNameToVerify))
                {
                    // http://pytools.codeplex.com/workitem/497
                    // .foo is ok
                    return false;
                }
            }

            // We verify CLOCK$ outside the regex since for some reason the regex is not matching the clock\\$ added.
            if (String.Equals(fileNameToVerify, "CLOCK$", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return _unsafeFileNameCharactersRegex.IsMatch(fileNameToVerify);
        }


        /// <summary>
        ///     Canonicalizes a file name, including:
        ///     - determines the full path to the file
        ///     - casts to upper case
        ///     Canonicalizing a file name makes it possible to compare file names using simple simple string comparison.
        ///     Note: this method does not handle shared drives and UNC drives.
        /// </summary>
        /// <param name="anyFileName">A file name, which can be relative/absolute and contain lower-case/upper-case characters.</param>
        /// <returns>Canonicalized file name.</returns>
        internal static string CanonicalizeFileName(string anyFileName)
        {
            // Get absolute path
            // Note: this will not handle UNC paths
            var fileInfo = new FileInfo(anyFileName);
            string fullPath = fileInfo.FullName;

            // Cast to upper-case
            fullPath = fullPath.ToUpperInvariant();

            return fullPath;
        }

        /// <summary>
        ///     Determines if a file is a template.
        /// </summary>
        /// <param name="fileName">The file to check whether it is a template file</param>
        /// <returns>true if the file is a template file</returns>
        internal static bool IsTemplateFile(string fileName)
        {
            if (String.IsNullOrEmpty(fileName))
            {
                return false;
            }

            string extension = Path.GetExtension(fileName);
            return (String.Equals(extension, ".vstemplate", StringComparison.OrdinalIgnoreCase) || string.Equals(extension, ".vsz", StringComparison.OrdinalIgnoreCase));
        }
    }
}