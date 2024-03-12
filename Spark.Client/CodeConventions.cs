using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeCasing
{
    /*
     * Avoid regions - they hurt readability
     * 
     * Avoid underscores - use PascalCase instead
     * 
     * PascalCase for private and public members
     * 
     * camelCase for variables and method arguments
     * 
     * underscore to identify property backing fields
     * 
     * Q: Why use uppercase for private members?
     * A: This way you always know the scope of a variable
     *    lowercase = local scope. Uppercase = class scope
     */


    public interface IExample { }

    public class CodeConventions
    {
        private int IntValue;

        public float FloatValue;

        private float _myPropertyBackingField;

        public float PropertyValue
        {
            get { return _myPropertyBackingField; }
            set { _myPropertyBackingField = value; }
        }

        public void DoSomething(int myParamater)
        {
            int myVariable = 0;
        }

        private void MyPrivateMethod()
        {

        }
    }
}
