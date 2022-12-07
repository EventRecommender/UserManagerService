CREATE TABLE user(
    id INT NOT NULL,
    username VARCHAR(255) NOT NULL,
    city VARCHAR(255) NOT NULL,
    institute VARCHAR(255) NOT NULL,
    role VARCHAR(255) NOT NULL,
    PRIMARY KEY (id)
);

CREATE TABLE password(
    userid INT NOT NULL,
    password VARCHAR(255) NOT NULL,
    PRIMARY KEY (userid),
    FOREIGN KEY (userid) REFERENCES user(id)
);