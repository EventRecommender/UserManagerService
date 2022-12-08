# UserMangerService
This is an microservice responsible for handling users in the [EventRecommender application](https://github.com/EventRecommender).

## Contents
#### [Endpoints](#endpoints)
- Login
- Fetch
- Verify
- Create
- Delete

# Endpoints

## Login
Checks if the username exists in the database, and the password is correct. 

### Input
\<string\> Username \
\<string\> password 

### Returns
An object containing _UserID_, _Username_, _Role_ and _JWT_.

```
  "Id": <int>,
  "Username": <string>,
  "Role": <string>,
  "Token": <string>
```

## /fetch
Retrieves a user from the database.

### Input
\<Int\> userID

### Returns
if user does not exist, an error message is returned. ("Not unique id", or "unavailable")\

else a user object is returned.

```
  "ID": <Int>,
  "username": <string>,
  "city": <string>
  "institute": <string>,
  "role": <string>
```

## /verify
checks if the request contains a valid token.

### Returns
true if the request contains a valid JWT in the authorization header.\
false if the request does not contain a valid JWT in the authorization header.

## /create
Creates a new user in the database.

### Input
\<string\> username - The wanted username\
\<string\> password - The wanted password, hashed and salted.\
\<string\> city - The city which the user is interested in.\
\<string\> institute - The institute which the user is connected to.\
\<string\> role - the requested role of the user\

### Returns
true if the user has successfully been added to the database.

## /delete
Removes a user from the database

### Input
<int> userID

### Returns
true if user is successfully removed.
false if an error has occured during the deletion process.
