<%@ Page Language="C#" %>
<script runat="server">
    protected void Page_Load(object sender, System.EventArgs e)
    {
        Response.Redirect("~/swagger", true);
    }
</script>
