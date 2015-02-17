// Copyright (c) 2015, Outercurve Foundation.
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification,
// are permitted provided that the following conditions are met:
//
// - Redistributions of source code must  retain  the  above copyright notice, this
//   list of conditions and the following disclaimer.
//
// - Redistributions in binary form  must  reproduce the  above  copyright  notice,
//   this list of conditions  and  the  following  disclaimer in  the documentation
//   and/or other materials provided with the distribution.
//
// - Neither  the  name  of  the  Outercurve Foundation  nor   the   names  of  its
//   contributors may be used to endorse or  promote  products  derived  from  this
//   software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING,  BUT  NOT  LIMITED TO, THE IMPLIED
// WARRANTIES  OF  MERCHANTABILITY   AND  FITNESS  FOR  A  PARTICULAR  PURPOSE  ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR
// ANY DIRECT, INDIRECT, INCIDENTAL,  SPECIAL,  EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO,  PROCUREMENT  OF  SUBSTITUTE  GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)  HOWEVER  CAUSED AND ON
// ANY  THEORY  OF  LIABILITY,  WHETHER  IN  CONTRACT,  STRICT  LIABILITY,  OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE)  ARISING  IN  ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;

namespace WebsitePanel.Portal
{
    public partial class ItemButtonPanel : WebsitePanelControlBase
    {
        public bool ButtonSaveVisible
        {
            set { btnSave.Visible = value; }
            get { return btnSave.Visible; }
        }

        public bool ButtonSaveExitVisible
        {
            set { btnSaveExit.Visible = value; }
            get { return btnSaveExit.Visible; }
        }

        public string ValidationGroup
        {
            set { 
                btnSave.ValidationGroup = value;
                btnSaveExit.ValidationGroup = value;
            }
            get { return btnSave.ValidationGroup; }
        }

        public string OnSaveClientClick
        {
            set
            {
                btnSave.OnClientClick = value;
                btnSaveExit.OnClientClick = value;
            }
        }


        public event EventHandler SaveClick = null;
        protected void btnSave_Click(object sender, EventArgs e)
        {
            if (SaveClick!=null)
            {
                SaveClick(this, e);
            }
        }

        public event EventHandler SaveExitClick = null;
        protected void btnSaveExit_Click(object sender, EventArgs e)
        {
            if (SaveExitClick!=null)
            {
                SaveExitClick(this, e);
            }
        }

    }
}