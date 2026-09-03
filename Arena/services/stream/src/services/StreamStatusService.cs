/* 
    A small dedicated service encapsulating "resolve stream_id from the webhook's broadcaster info → determine Live vs Ended from event type → call StreamRepository (and MatchRepository if cascading) with the conditional update." Keeps your webhook handler thin and makes this logic independently unit-testable, rather than burying it inline in the controller


*/