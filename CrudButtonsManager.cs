using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Bus_coursework
{
    public class CrudButtonsManager
    {
        private readonly Button _btnAdd;
        private readonly Button _btnEdit;
        private readonly Button _btnDelete;

        public CrudButtonsManager(Button btnAdd, Button btnEdit, Button btnDelete)
        {
            _btnAdd = btnAdd;
            _btnEdit = btnEdit;
            _btnDelete = btnDelete;
        }

        public void Show()
        {
            SetVisible(true);
        }

        public void Hide()
        {
            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            _btnAdd.Visible = visible;
            _btnEdit.Visible = visible;
            _btnDelete.Visible = visible;
        }

        public void ApplyPermissions(string roleName) 
        {
            roleName = roleName?.ToLowerInvariant();

            bool canAddEdit = false;
            bool canDelete = false;

            switch (roleName)
            {
                case "director":
                case "dispatcher":
                case "hr_manager":
                    canAddEdit = true;
                    canDelete = true;
                    break;

                case "engineer":
                case "guest":
                default:
                    canAddEdit = false;
                    canDelete = false;
                    break;
            }

            _btnAdd.Visible = canAddEdit;
            _btnEdit.Visible = canAddEdit;
            _btnDelete.Visible = canDelete;

            _btnAdd.Enabled = canAddEdit;
            _btnEdit.Enabled = canAddEdit;
            _btnDelete.Enabled = canDelete;
        }
    }
}
