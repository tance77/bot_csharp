<%@ Page Title="" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Spotify.aspx.cs" Inherits="fembot.Spotify" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <br />
    <div class="row">
        <div class="col-md-3">
            <iframe src="https://embed.spotify.com/?uri=spotify:track:4th1RQAelzqgY7wL53UGQt" width="300" height="380" frameborder="0" allowtransparency="true"></iframe>

        </div>

        <div class="col-lg-9 col-xs-push-custom">
            <asp:Timer runat="server" ID="Timer1" Interval="10000" OnTick="SongUpdateTimer"></asp:Timer>
            <asp:UpdatePanel ID="UpdatePanel1" runat="server">
                <Triggers>
                    <asp:AsyncPostBackTrigger ControlID="Timer1"/>
                </Triggers>
                <ContentTemplate>
                    <asp:GridView ID="GridView1" runat="server" DataSourceID="fembot" AllowPaging="True" AutoGenerateColumns="False" CssClass="table table-bordered table-striped table-hover" DataKeyNames="rowid">
                        <Columns>
                            <asp:BoundField DataField="rowid" HeaderText="ID" ReadOnly="True" SortExpression="rowid" />
                            <asp:BoundField DataField="title" HeaderText="Title" SortExpression="title" />
                            <asp:BoundField DataField="artist" HeaderText="Artist" SortExpression="artist" />
                            <asp:BoundField DataField="durration" HeaderText="Durration" SortExpression="durration" />
                            <asp:BoundField DataField="requested_by" HeaderText="Requested By" SortExpression="requested_by" />
                            <asp:HyperLinkField DataNavigateUrlFields="url" DataTextField="url" HeaderText="URL" />
                        </Columns>
                        <HeaderStyle CssClass="success" />
                    </asp:GridView>
                    <asp:SqlDataSource ID="fembot" runat="server" ConnectionString="<%$ ConnectionStrings:ConnectionString %>" ProviderName="<%$ ConnectionStrings:ConnectionString.ProviderName %>" SelectCommand="SELECT rowid, requested_by, durration, artist, title, url FROM Songs WHERE (channel_name = &quot;jchinn&quot;)"></asp:SqlDataSource>
                </ContentTemplate>
            </asp:UpdatePanel>
        </div>
    </div>

</asp:Content>
