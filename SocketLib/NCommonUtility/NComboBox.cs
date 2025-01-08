using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NCommonUtility
{
    public class NComboBox : System.Windows.Forms.ComboBox
    {
        [Serializable]
        private class Item
        {
            public string display { get; private set; }
            public Object value { get; private set; }
            public Item(string display, Object value)
            {
                this.display = display;
                this.value = value;
            }
            public override string ToString()
            {
                return display;
            }
        }

        bool isInit = false;
        List<Item> item_list = new List<Item>();

        public NComboBox()
        {
            this.DisplayMember = "display";
            this.ValueMember = "value";
        }

        [Category("カスタマイズ")]
        [Description("falseの時テキストの編集を許可しない")]
        [DefaultValue(false)]
        public bool AllowEdit 
        {
            get => (DropDownStyle == ComboBoxStyle.DropDown);
            set
            {
                if (value == true)
                {
                    DropDownStyle = ComboBoxStyle.DropDown;
                }
                else
                {
                    DropDownStyle = ComboBoxStyle.DropDownList;
                }
            }
        }


        [Browsable(false)]
        public new string DisplayMember
        {
            get => base.DisplayMember;
            set => base.DisplayMember = value;
        }
        [Browsable(false)]
        public new string ValueMember
        {
            get => base.ValueMember;
            set => base.ValueMember = value;
        }
        [Browsable(false)]
        public new ObjectCollection Items
        {
            get => base.Items;
        }
        [Browsable(false)]
        public new object DataSource
        {
            get => base.DataSource;
            set => base.DataSource = value;
        }

        [Browsable(false)]
        public new ComboBoxStyle DropDownStyle
        {
            get => base.DropDownStyle;
            set => base.DropDownStyle = value;
        }

        public void AddItem(string disp, Object val)
        {
            Item item = new Item(disp, val);
            item_list.Add(item);
            if (isInit == true)
            {
                this.DataSource = null;
                this.DataSource = item_list;
            }
        }
        public void ClearItems()
        {
            item_list.Clear();
            if (isInit == true)
            {
                this.DataSource = null;
            }
        }

        public string GetSelectedDisplay()
        {
            return ((Item)SelectedItem).display;
        }

        protected override void OnCreateControl()
        {
            if (isInit == false)
            {
                isInit = true;
                if (DesignMode == false)
                {
                    this.DataSource = item_list;
                }
            }
            base.OnCreateControl();
        }
    }
}
