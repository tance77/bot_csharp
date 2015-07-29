<%@ Page Title="" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Spotify.aspx.cs" Inherits="fembot.Spotify" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <script>
        
        function buildTable() {

            var song = $('#ello').val();

            $.ajax({
                url: 'Songs.asmx?op=GetSongByChannelName',
                data: 'jchinn',
                dataType: "xml"
            });

            var url = "Songs.asmx?op=GetSongByChannelName";
            $.getJSON(url, function (response) {
                var write;
                $.each(response, function (index, table) {
                    write += "<tr><td>" + item.Title + "</td><td>" + item.Artist + "</td><td>" + item.Durration + "</td><td>" + item.RequestedBy + "</td><td>" + item.Url + "</td></tr>";
                    if (table.status === true) {
                        write += '<td class="ap">Aprovado</td>';
                    } else {
                        write += '<td class="ng">Negado</td>';
                    }
                    write += '<td>' + table.id + '</td><td><button class="bt_delete">Deletar</button></td></tr>';
                }); //end each
                $('#songs').html(write);
            }); //end getJSON

        }

        $(document).ready(function () {
            var refresh = setInterval(function () {
                buildTable();
            }, 10000);
        });


    </script>
    
    <br />
    <div class="row">
        <div class="col-md-3">
            <iframe src="https://embed.spotify.com/?uri=spotify:track:4th1RQAelzqgY7wL53UGQt" width="300" height="380" frameborder="0" allowtransparency="true"></iframe>
        </div>

        <div class="col-lg-9 col-xs-push-custom">
            
            <table class="table table-bordered table-striped table-hover" id ="ello">
                <thead id ="songs">
                    <tr class="success">
                        <th>Title</th>
                        <th>Artist</th>
                        <th>Durration</th>
                        <th>Requested by</th>
                        <th>URL</th>
                    </tr>
                </thead>
                <tbody>
                    <tr>
                        <td>The Nights</td>
                        <td>Avicii</td>
                        <td>2:56</td>
                        <td>BlackMarmalade</td>
                        <td>BlackMarmalade</td>
                    </tr>
                    <tr>
                        <td>Test</td>
                        <td>Test2</td>
                        <td>Test3</td>
                        <td>Test4</td>
                        <td>BlackMarmalade</td>
                    </tr>
                </tbody>
            </table>

        </div>
    </div>

</asp:Content>
