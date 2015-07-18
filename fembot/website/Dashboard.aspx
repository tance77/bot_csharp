<%@ Page Title="Dashboard" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Dashboard.aspx.cs" Inherits="fembot.Dashboard" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
<div class="row">
        <div class="col-md-4">
            <img src="/images/dashboard.png" class ="home_image_dashboard"/>
            <h2>Conection Status</h2>
            <p>
                Each user will have their own unique dashboard where they may control their own unique version of their bot. 
            </p>
            <p>
                <a class="btn btn-default" href="Dashboard.aspx">Learn more &raquo;</a>
            </p>
        </div>
        <div class="col-md-4">
            <img src="/images/spotify.png" class="home_image_dashboard" />
            <h2>Spotify Intergration</h2>
            <p>
                Spotify has been intergrated to allow for song request through Spotify. Each user will have their own unique playlist provided by Spotify. Spotify premium is higly encouraged.
            </p>
            <p>
                <a class="btn btn-default" href="Spotify.aspx">Learn more &raquo;</a>
            </p>
        </div>
        <div class="col-md-4">
            <img src="/images/setttings.png" class="home_image_dashboard" />
            <h2>Spam Filters</h2>
            <p>
                You can easily customize advance spam filters to get rid of annoying users spam and links.
            </p>
            <p>
                <a class="btn btn-default" href="Filters.aspx">Learn more &raquo;</a>
            </p>
        </div>
    </div>
</asp:Content>