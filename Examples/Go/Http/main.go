package main

import (
	"bytes"
	"encoding/json"
	"io/ioutil"
	"log"
	"net/http"
)

func do_get() {
	resp, err := http.Get("https://jsonplaceholder.typicode.com/posts/1")
	if err != nil {
		log.Fatalln(err)
	}
	defer resp.Body.Close()
	body, err := ioutil.ReadAll(resp.Body)
	if err != nil {
		log.Fatalln(err)
	}
	sb := string(body)
	log.Printf(sb)
}

func do_post() {
	postBody, _ := json.Marshal(map[string]string{
		"name":  "Anon",
		"email": "Anon@white-house.gov",
	})
	responseBody := bytes.NewBuffer(postBody)
	resp, err := http.Post("https://postman-echo.com/post", "application/json", responseBody)
	if err != nil {
		log.Fatalf("An error occured %v", err)
	}
	defer resp.Body.Close()
	body, err := ioutil.ReadAll(resp.Body)
	if err != nil {
		log.Fatalln(err)
	}
	sb := string(body)
	log.Printf(sb)
}

func main() {
	do_get()
	do_post()
}
