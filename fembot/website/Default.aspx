<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="fembot._Default" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <div class="jumbotron">
        <h1>Fembot/Chinnbot</h1>
        <p class="lead">Fembot is a Twitch.tv moderation bot that I created in my free time to help streamers grow their streams. You can have more access to things that im working on by accessing my GitHub.</p>
        <p><a href="http://www.github.com/tance77" class="btn btn-primary btn-large">Learn more &raquo;</a></p>
    </div>

    <div class="row">
        <div class="col-md-4">
            <img src="/images/dashboard.png" class ="home_image_dashboard"/>
            <h2>Dashboard</h2>
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
