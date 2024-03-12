using System;
using System.ComponentModel;
using System.Reflection;
using System.Xml.Serialization;

namespace Spark
{
    public class Field
    {
        public MemberInfo Member;

        private PropertyInfo prop;
        private FieldInfo field;

        public bool CanWrite { get; private set; }
        public bool Ignored { get; private set; }
        public bool Browsable { get; private set; }

        public string Category = string.Empty;
        public string Description = string.Empty;

        public bool IsPublic { get; private set; }

        public Field(MemberInfo member)
        {
            Browsable = true;
            Member = member;

            field = member as FieldInfo;
            prop = member as PropertyInfo;

            if (prop != null)
                CanWrite = prop.GetSetMethod() != null;

            if (field != null)
                CanWrite = !field.IsInitOnly;

            Ignored = member.IsDefined(typeof(XmlIgnoreAttribute), true);

            if (field != null && (field.IsInitOnly || field.IsLiteral))
                Ignored = true;

            BrowsableAttribute browsable = GetAttribute<BrowsableAttribute>();
            if (browsable != null) Browsable = browsable.Browsable;

            if (prop != null)
                IsPublic = true;

            if (field != null)
                IsPublic = field.IsPublic;

            if(IsInternal(member)) IsPublic = false;
            
        }

        public string Name
        {
            get { return Member.Name; }
        }

        public Type Type
        {
            get
            {
                if (field != null) return field.FieldType;
                if (prop != null) return prop.PropertyType;

                return null;
            }
        }

        public Type DeclaringType => Member.DeclaringType;

        public K GetAttribute<K>() where K : Attribute
        {
            object[] att = Member.GetCustomAttributes(typeof(K), false);
            if (att.Length > 0)
                return att[0] as K;

            return null;
        }

        public object[] GetCustomAttributes(Type type, bool inherit)
        {
            if (field != null) return field.GetCustomAttributes(type, inherit);
            if (prop != null) return prop.GetCustomAttributes(type, inherit);

            return null;
        }

        public new Type GetType()
        {
            if (field != null) return field.FieldType;
            if (prop != null) return prop.PropertyType;

            return Member.ReflectedType;
        }

        public object GetValue(object instance)
        {
            if (field != null) return field.GetValue(instance);
            if (prop != null) return prop.GetValue(instance, null);

            return null;
        }

        public void SetValue(object instance, object value)
        {
            if (field != null) field.SetValue(instance, value);
            if (prop != null) prop.SetValue(instance, value, null);
        }

        bool IsInternal(MemberInfo memberInfo)
        {
            if (memberInfo == null)
            {
                return false;
                throw new ArgumentNullException(nameof(memberInfo));
            }

            // Check the accessibility of the member based on its type
            switch (memberInfo)
            {
                case FieldInfo fieldInfo:
                    return fieldInfo.IsAssembly; // IsAssembly means internal
                case PropertyInfo propertyInfo:
                    return IsInternal(propertyInfo.GetGetMethod(true)) || IsInternal(propertyInfo.GetSetMethod(true));
                case MethodInfo methodInfo:
                    return methodInfo.IsAssembly; // IsAssembly means internal
              // Add more cases for other member types if needed
                default:
                    return false; // Unknown member type, assume not internal
            }
        }
    }
}