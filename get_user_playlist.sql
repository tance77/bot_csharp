CREATE PROCEDURE `get_user_playlist` (IN chan varchar(100))
BEGIN

SELECT * 
FROM   songs 
WHERE  channel_name = chan; 

END
