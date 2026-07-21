using System;
using System.Collections.Generic;
using DBTaskMan;
using System.Text;

namespace TaskMan
{
    class FormComponentMerge : FormMerge
    {

        public FormComponentMerge(Component currentComponent, Component newComponent) :
            base(currentComponent, newComponent)
        {
            AddField("Name", "Name", null);

            Text = "Merge conflicting changes: Component '" + currentComponent.FullName + "'";

        }
    }
}
