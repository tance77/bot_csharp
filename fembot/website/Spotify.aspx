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
                    <asp:GridView ID="GridView1" runat="server" DataSourceID="fembot" AllowPaging="True" AutoGenerateColumns="False" CssClass="table table-bordered table-striped table-hover" DataKeyNames="id">
                        <Columns>
                            <asp:BoundField DataField="channel_name" HeaderText="channel_name" SortExpression="channel_name" />
                            <asp:BoundField DataField="requested_by" HeaderText="requested_by" SortExpression="requested_by" />
                            <asp:BoundField DataField="song_id" HeaderText="song_id" SortExpression="song_id" />
                            <asp:BoundField DataField="duration" HeaderText="duration" SortExpression="duration" />
                            <asp:BoundField DataField="artist" HeaderText="artist" SortExpression="artist" />
                            <asp:BoundField DataField="title" HeaderText="title" SortExpression="title" />
                            <asp:BoundField DataField="url" HeaderText="url" SortExpression="url" />
                            <asp:BoundField DataField="id" HeaderText="id" ReadOnly="True" SortExpression="id" />
                        </Columns>
                        <HeaderStyle CssClass="success" />
                    </asp:GridView>
                    <asp:SqlDataSource ID="fembot" runat="server" ConnectionString="<%$ ConnectionStrings:ConnectionString %>" ProviderName="<%$ ConnectionStrings:ConnectionString.ProviderName %>" SelectCommand="SELECT * FROM [Songs]"></asp:SqlDataSource>
                </ContentTemplate>
            </asp:UpdatePanel>
        </div>
    </div>

</asp:Content>
