using System;
using System.Collections.Generic;
using DebugEngine.Node;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;

namespace DebugEngine.Engine
{
    // An implementation of IDebugProperty2
    // This interface represents a stack frame property, a program document property, or some other property. 
    // The property is usually the result of an expression evaluation. 
    //
    // The sample engine only supports locals and parameters for functions that have symbols loaded.
    internal class AD7Property : IDebugProperty3
    {
        private readonly NodeEvaluationResult _variable;

        public AD7Property(NodeEvaluationResult variable)
        {
            _variable = variable;
        }

        // Construct a DEBUG_PROPERTY_INFO representing this local or parameter.
        public DEBUG_PROPERTY_INFO ConstructDebugPropertyInfo(uint radix, enum_DEBUGPROP_INFO_FLAGS dwFields)
        {
            var propertyInfo = new DEBUG_PROPERTY_INFO();

            if (dwFields.HasFlag(enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_FULLNAME))
            {
                propertyInfo.bstrFullName = _variable.FullName;
                propertyInfo.dwFields |= enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_FULLNAME;
            }

            if (dwFields.HasFlag(enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_NAME))
            {
                propertyInfo.bstrName = _variable.Name;
                propertyInfo.dwFields |= enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_NAME;
            }

            if (dwFields.HasFlag(enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_TYPE))
            {
                propertyInfo.bstrType = _variable.TypeName;
                propertyInfo.dwFields |= enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_TYPE;
            }

            if (dwFields.HasFlag(enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_VALUE))
            {
                if (radix == 16)
                {
                    propertyInfo.bstrValue = _variable.HexValue ?? _variable.StringValue;
                }
                else
                {
                    if (_variable.Type.HasFlag(NodeExpressionType.String))
                    {
                        propertyInfo.bstrValue = string.Format("\"{0}\"", _variable.StringValue);
                    }
                    else
                    {
                        propertyInfo.bstrValue = _variable.StringValue;
                    }
                }

                propertyInfo.dwFields |= enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_VALUE;
            }

            if (dwFields.HasFlag(enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_ATTRIB))
            {
                if (_variable.Type.HasFlag(NodeExpressionType.ReadOnly))
                {
                    propertyInfo.dwAttrib |= enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_VALUE_READONLY;
                }

                if (_variable.Type.HasFlag(NodeExpressionType.Private))
                {
                    propertyInfo.dwAttrib |= enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_ACCESS_PRIVATE;
                }

                if (_variable.Type.HasFlag(NodeExpressionType.Expandable))
                {
                    propertyInfo.dwAttrib |= enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_OBJ_IS_EXPANDABLE;
                }

                if (_variable.Type.HasFlag(NodeExpressionType.String))
                {
                    propertyInfo.dwAttrib |= enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_VALUE_RAW_STRING;
                }

                if (_variable.Type.HasFlag(NodeExpressionType.Boolean))
                {
                    propertyInfo.dwAttrib |= enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_VALUE_BOOLEAN;
                }

                if (_variable.Type.HasFlag(NodeExpressionType.Property))
                {
                    propertyInfo.dwAttrib |= enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_PROPERTY;
                }

                if (_variable.Type.HasFlag(NodeExpressionType.Function))
                {
                    propertyInfo.dwAttrib |= enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_METHOD;
                }
            }

            // Always provide the property so that we can access locals from the automation object.
            propertyInfo.pProperty = this;
            propertyInfo.dwFields |= enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_PROP;

            return propertyInfo;
        }

        #region IDebugProperty2 Members

        // Enumerates the children of a property. This provides support for dereferencing pointers, displaying members of an array, or fields of a class or struct.
        // The sample debugger only supports pointer dereferencing as children. This means there is only ever one child.
        private bool _stringValueLoaded;

        public int EnumChildren(enum_DEBUGPROP_INFO_FLAGS dwFields, uint dwRadix, ref Guid guidFilter, enum_DBG_ATTRIB_FLAGS dwAttribFilter, string pszNameFilter, uint dwTimeout,
                                out IEnumDebugPropertyInfo2 ppEnum)
        {
            ppEnum = null;
            IList<NodeEvaluationResult> children = _variable.Children;
            if (children == null)
            {
                return VSConstants.S_FALSE;
            }

            var properties = new DEBUG_PROPERTY_INFO[children.Count];
            for (int i = 0; i < children.Count; i++)
            {
                properties[i] = new AD7Property(children[i]).ConstructDebugPropertyInfo(dwRadix, dwFields);
            }

            ppEnum = new AD7PropertyEnum(properties);
            return VSConstants.S_OK;
        }

        // Returns the property that describes the most-derived property of a property
        // This is called to support object oriented languages. It allows the debug engine to return an IDebugProperty2 for the most-derived 
        // object in a hierarchy. This engine does not support this.
        public int GetDerivedMostProperty(out IDebugProperty2 ppDerivedMost)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        // This method exists for the purpose of retrieving information that does not lend itself to being retrieved by calling the IDebugProperty2::GetPropertyInfo 
        // method. This includes information about custom viewers, managed type slots and other information.
        // The sample engine does not support this.
        public int GetExtendedInfo(ref Guid guidExtendedInfo, out object pExtendedInfo)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        // Returns the memory bytes for a property value.
        public int GetMemoryBytes(out IDebugMemoryBytes2 ppMemoryBytes)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        // Returns the memory context for a property value.
        public int GetMemoryContext(out IDebugMemoryContext2 ppMemory)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        // Returns the parent of a property.
        // The sample engine does not support obtaining the parent of properties.
        public int GetParent(out IDebugProperty2 ppParent)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        // Fills in a DEBUG_PROPERTY_INFO structure that describes a property.
        public int GetPropertyInfo(enum_DEBUGPROP_INFO_FLAGS dwFields, uint dwRadix, uint dwTimeout, IDebugReference2[] rgpArgs, uint dwArgCount, DEBUG_PROPERTY_INFO[] pPropertyInfo)
        {
            pPropertyInfo[0] = new DEBUG_PROPERTY_INFO();
            pPropertyInfo[0] = ConstructDebugPropertyInfo(dwRadix, dwFields);
            return VSConstants.S_OK;
        }

        //  Return an IDebugReference2 for this property. An IDebugReference2 can be thought of as a type and an address.
        public int GetReference(out IDebugReference2 ppReference)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        // Returns the size, in bytes, of the property value.
        public int GetSize(out uint pdwSize)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        // The debugger will call this when the user tries to edit the property's values
        // We only accept setting values as strings
        public int SetValueAsReference(IDebugReference2[] rgpArgs, uint dwArgCount, IDebugReference2 pValue, uint dwTimeout)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        // The debugger will call this when the user tries to edit the property's values in one of the debugger windows.
        public int SetValueAsString(string pszValue, uint dwRadix, uint dwTimeout)
        {
            try
            {
                _variable.StackFrame.SetValueAsync(_variable, pszValue).Wait();
            }
            catch (Exception)
            {
                return VSConstants.E_FAIL;
            }

            return VSConstants.S_OK;
        }

        public int GetStringCharLength(out uint pLen)
        {
            pLen = (uint) _variable.StringLength;
            return VSConstants.S_OK;
        }

        public int GetStringChars(uint buflen, ushort[] rgString, out uint pceltFetched)
        {
            pceltFetched = buflen;

            if (!_stringValueLoaded)
            {
                NodeEvaluationResult result = _variable.Debugger.EvaluateVariableAsync(_variable).Result;
                if (result == null)
                {
                    pceltFetched = 0;
                    return VSConstants.E_FAIL;
                }

                _variable.StringValue = result.StringValue;
                _stringValueLoaded = true;
            }

            _variable.StringValue.ToCharArray().CopyTo(rgString, 0);

            return VSConstants.S_OK;
        }

        public int CreateObjectID()
        {
            throw new NotImplementedException();
        }

        public int DestroyObjectID()
        {
            throw new NotImplementedException();
        }

        public int GetCustomViewerCount(out uint pcelt)
        {
            throw new NotImplementedException();
        }

        public int GetCustomViewerList(uint celtSkip, uint celtRequested, DEBUG_CUSTOM_VIEWER[] rgViewers, out uint pceltFetched)
        {
            throw new NotImplementedException();
        }

        public int SetValueAsStringWithError(string pszValue, uint dwRadix, uint dwTimeout, out string errorString)
        {
            errorString = "Unable to set value.";

            try
            {
                _variable.StackFrame.SetValueAsync(_variable, pszValue).Wait();
            }
            catch (AggregateException e)
            {
                var baseException = e.GetBaseException();
                if (!string.IsNullOrEmpty(baseException.Message))
                {
                    errorString = baseException.Message;
                }

                return VSConstants.E_FAIL;
            }
            catch (Exception)
            {
                return VSConstants.E_FAIL;
            }

            return VSConstants.S_OK;
        }

        #endregion
    }
}