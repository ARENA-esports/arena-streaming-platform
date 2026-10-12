-- Ensure match 1 and stream 101 are Live so viewers can watch and earn coins immediately
UPDATE matches SET status = 'Live', scheduled_time = NOW() WHERE match_id = 1;
UPDATE streams SET status = 'Live', started_at = UTC_TIMESTAMP() WHERE stream_id = 101;
