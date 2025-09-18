using System;
using System.Web.UI;

using HostBridge.AspNet;
using HostBridge.Examples.Common;

namespace WebForms;

public partial class Default : Page
{
    [FromServices] public IMyScoped? MyScoped { get; set; }
    
    protected void Page_Load(object sender, EventArgs e)
    {
        Response.Output.WriteLine($"{nameof(MyScoped)}.Id: {MyScoped?.Id:B}");
    }
}