using System;
using System.Collections.Generic;
using Android.Content;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;

namespace MapboxNavigation.UI.Droid.TestApp
{
    public class ExamplesAdapter : RecyclerView.Adapter
    {
        private LayoutInflater inflater;
        private List<SampleItem> itemList;
        private Action<int> itemClickAction;

        public ExamplesAdapter(Context appContext, Action<int> itemClickAction)
        {
            inflater = LayoutInflater.From(appContext);
            itemList = new List<SampleItem>();
            this.itemClickAction = itemClickAction;
        }

        public void AddSampleItems(List<SampleItem> list)
        {
            itemList.AddRange(list);
            NotifyItemRangeInserted(0, list.Count);
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var view = inflater.Inflate(Resource.Layout.item_examples_adapter, parent, false);
            return new ExamplesViewHolder(view, itemClickAction);
        }

        public override int ItemCount => itemList.Count;

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var vh = holder as ExamplesViewHolder;
            vh.BindItem(itemList[position]);
        }

        private class ExamplesViewHolder : RecyclerView.ViewHolder
        {
            public TextView nameView;
            public TextView descriptionView;
            private Action<int> itemClickAction;
            private View view;

            public ExamplesViewHolder(View itemView, Action<int> itemClickAction) : base(itemView)
            {
                this.itemClickAction = itemClickAction;
                view = itemView;
                nameView = itemView.FindViewById<TextView>(Resource.Id.nameView);
                descriptionView = itemView.FindViewById<TextView>(Resource.Id.descriptionView);
            }

            public void BindItem(SampleItem sampleItem)
            {
                nameView.Text = sampleItem.Name;
                descriptionView.Text = sampleItem.Description;

                view.Click += (s, e) => itemClickAction?.Invoke(LayoutPosition);
            }
        }
    }
}
