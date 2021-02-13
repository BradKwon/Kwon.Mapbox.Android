using System;

namespace MapboxNavigation.UI.Droid.TestApp.Legacy
{
    public class SampleItem
    {
        public string Name { get; }
        public string Description { get; }
        public Type Activity { get; }

        public SampleItem(string name, string description, Type activity)
        {
            Name = name;
            Description = description;
            Activity = activity;
        }
    }
}
