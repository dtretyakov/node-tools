/********************************************************************************************

Copyright (c) Microsoft Corporation 
All rights reserved. 

Microsoft Public License: 

This license governs use of the accompanying software. If you use the software, you 
accept this license. If you do not accept the license, do not use the software. 

1. Definitions 
The terms "reproduce," "reproduction," "derivative works," and "distribution" have the 
same meaning here as under U.S. copyright law. 
A "contribution" is the original software, or any additions or changes to the software. 
A "contributor" is any person that distributes its contribution under this license. 
"Licensed patents" are a contributor's patent claims that read directly on its contribution. 

2. Grant of Rights 
(A) Copyright Grant- Subject to the terms of this license, including the license conditions 
and limitations in section 3, each contributor grants you a non-exclusive, worldwide, 
royalty-free copyright license to reproduce its contribution, prepare derivative works of 
its contribution, and distribute its contribution or any derivative works that you create. 
(B) Patent Grant- Subject to the terms of this license, including the license conditions 
and limitations in section 3, each contributor grants you a non-exclusive, worldwide, 
royalty-free license under its licensed patents to make, have made, use, sell, offer for 
sale, import, and/or otherwise dispose of its contribution in the software or derivative 
works of the contribution in the software. 

3. Conditions and Limitations 
(A) No Trademark License- This license does not grant you rights to use any contributors' 
name, logo, or trademarks. 
(B) If you bring a patent claim against any contributor over patents that you claim are 
infringed by the software, your patent license from such contributor to the software ends 
automatically. 
(C) If you distribute any portion of the software, you must retain all copyright, patent, 
trademark, and attribution notices that are present in the software. 
(D) If you distribute any portion of the software in source code form, you may do so only 
under this license by including a complete copy of this license with your distribution. 
If you distribute any portion of the software in compiled or object code form, you may only 
do so under a license that complies with this license. 
(E) The software is licensed "as-is." You bear the risk of using it. The contributors give 
no express warranties, guarantees or conditions. You may have additional consumer rights 
under your local laws which this license cannot change. To the extent permitted under your 
local laws, the contributors exclude the implied warranties of merchantability, fitness for 
a particular purpose and non-infringement.

********************************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using EnvDTE;

namespace Microsoft.VisualStudio.Project.Automation
{
    /// <summary>
    ///     Contains all of the properties of a given object that are contained in a generic collection of properties.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    [CLSCompliant(false), ComVisible(true)]
    public class OAProperties : Properties
    {
        #region fields

        private readonly Dictionary<string, Property> properties = new Dictionary<string, Property>();
        private readonly NodeProperties target;

        #endregion

        #region properties

        /// <summary>
        ///     Defines the NodeProperties object that contains the defines the properties.
        /// </summary>
        public NodeProperties Target
        {
            get { return target; }
        }

        /// <summary>
        ///     The hierarchy node for the object which properties this item represent
        /// </summary>
        public HierarchyNode Node
        {
            get { return Target.Node; }
        }

        /// <summary>
        ///     Defines a dictionary of the properties contained.
        /// </summary>
        public Dictionary<string, Property> Properties
        {
            get { return properties; }
        }

        #endregion

        #region ctor

        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public OAProperties(NodeProperties target)
        {
            Debug.Assert(target != null);

            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            this.target = target;
            AddPropertiesFromType(target.GetType());
        }

        #endregion

        #region EnvDTE.Properties

        /// <summary>
        ///     Microsoft Internal Use Only.
        /// </summary>
        public virtual object Application
        {
            get { return null; }
        }

        /// <summary>
        ///     Gets a value indicating the number of objects in the collection.
        /// </summary>
        public int Count
        {
            get { return properties.Count; }
        }

        /// <summary>
        ///     Gets the top-level extensibility object.
        /// </summary>
        public virtual DTE DTE
        {
            get
            {
                return UIThread.DoOnUIThread(delegate
                    {
                        if (target == null || target.Node == null || target.Node.ProjectMgr == null || target.Node.ProjectMgr.IsClosed ||
                            target.Node.ProjectMgr.Site == null)
                        {
                            throw new InvalidOperationException();
                        }
                        return target.Node.ProjectMgr.Site.GetService(typeof (DTE)) as DTE;
                    });
            }
        }

        /// <summary>
        ///     Gets an enumeration for items in a collection.
        /// </summary>
        /// <returns>An enumerator. </returns>
        public IEnumerator GetEnumerator()
        {
            if (properties == null)
            {
                yield return null;
            }

            if (properties.Count == 0)
            {
                yield return new OANullProperty(this);
            }

            IEnumerator enumerator = properties.Values.GetEnumerator();

            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        }

        /// <summary>
        ///     Returns an indexed member of a Properties collection.
        /// </summary>
        /// <param name="index">The index at which to return a mamber.</param>
        /// <returns>A Property object.</returns>
        [SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        public virtual Property Item(object index)
        {
            if (index is string)
            {
                var indexAsString = (string) index;
                if (properties.ContainsKey(indexAsString))
                {
                    return properties[indexAsString];
                }
            }
            else if (index is int)
            {
                int realIndex = (int) index - 1;
                if (realIndex >= 0 && realIndex < properties.Count)
                {
                    IEnumerator enumerator = properties.Values.GetEnumerator();

                    int i = 0;
                    while (enumerator.MoveNext())
                    {
                        if (i++ == realIndex)
                        {
                            return (Property) enumerator.Current;
                        }
                    }
                }
            }

            return new OANullProperty(this);
        }

        /// <summary>
        ///     Gets the immediate parent object of a Properties collection.
        /// </summary>
        public virtual object Parent
        {
            get { return null; }
        }

        #endregion

        #region methods

        /// <summary>
        ///     Add properties to the collection of properties filtering only those properties which are com-visible and AutomationBrowsable
        /// </summary>
        /// <param name="targetType">The type of NodeProperties the we should filter on</param>
        protected void AddPropertiesFromType(Type targetType)
        {
            Debug.Assert(targetType != null);

            if (targetType == null)
            {
                throw new ArgumentNullException("targetType");
            }

            // If the type is not COM visible, we do not expose any of the properties
            if (!IsComVisible(targetType))
                return;

            // Add all properties being ComVisible and AutomationVisible 
            PropertyInfo[] propertyInfos = targetType.GetProperties();
            foreach (PropertyInfo propertyInfo in propertyInfos)
            {
                if (!IsInMap(propertyInfo) && IsComVisible(propertyInfo) && IsAutomationVisible(propertyInfo))
                {
                    AddProperty(propertyInfo);
                }
            }
        }

        #endregion

        #region virtual methods

        /// <summary>
        ///     Creates a new OAProperty object and adds it to the current list of properties
        /// </summary>
        /// <param name="propertyInfo">The property to be associated with an OAProperty object</param>
        protected virtual void AddProperty(PropertyInfo propertyInfo)
        {
            if (propertyInfo == null)
            {
                throw new ArgumentNullException("propertyInfo");
            }

            properties.Add(propertyInfo.Name, new OAProperty(this, propertyInfo));
        }

        #endregion

        #region helper methods

        private bool IsInMap(PropertyInfo propertyInfo)
        {
            return properties.ContainsKey(propertyInfo.Name);
        }

        private static bool IsAutomationVisible(PropertyInfo propertyInfo)
        {
            object[] customAttributesOnProperty = propertyInfo.GetCustomAttributes(typeof (AutomationBrowsableAttribute), true);

            foreach (AutomationBrowsableAttribute attr in customAttributesOnProperty)
            {
                if (!attr.Browsable)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool IsComVisible(Type targetType)
        {
            object[] customAttributesOnProperty = targetType.GetCustomAttributes(typeof (ComVisibleAttribute), true);

            foreach (ComVisibleAttribute attr in customAttributesOnProperty)
            {
                if (!attr.Value)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool IsComVisible(PropertyInfo propertyInfo)
        {
            object[] customAttributesOnProperty = propertyInfo.GetCustomAttributes(typeof (ComVisibleAttribute), true);

            foreach (ComVisibleAttribute attr in customAttributesOnProperty)
            {
                if (!attr.Value)
                {
                    return false;
                }
            }
            return true;
        }

        #endregion
    }
}