﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//Implementation of the Observer Design Pattern.

//you can either use the generic Observable<T> class and do things small-scale, or you can have your script implement IObserver<Message>, which has automatic linking via singleton

//to use the singleton:
//  -have your monobehaviour implement IObserver<Message>, which requires you to implement a public void Notify(Message message) {}, in which you handle the incoming message
//  -call the line "Observers.Subscribe(this, params string[] messageTypes)". 
//          For example, the line of code to listen to messages tagged "AttackMessage" and "OptionsMessage" would be Observers.Subscribe(this, "AttackMessage", "OptionsMessage");
//  -If the script is ever deactivated or destroyed, call "Observers.Unsubscribe(this, params string[] messageTypes)". Same formatting as above

//to send a message:
//  -call Observers.Post(new Message([...]));
//          For example, to send a basic message tagged "AttackMessage", the code would be Observers.Post(new Message("AttackMessage"));

//note: I HEAVILY suggest you use the Tags class to set message types in code, instead of using string literals as in these examples
//I also advise that sub-classes of message (to carry data) should be written in a different file. It'll make it easier for you to update this file in case of changes

public class Observable<T>
{
    List<IObserver<T>> observers;

    //constructor
    public Observable()
    {
        observers = new List<IObserver<T>>();
    }

    public void Subscribe(IObserver<T> subscriber) { observers.Add(subscriber); }

    public void Unsubscribe(IObserver<T> subscriber) { observers.Remove(subscriber); }

    public void Post(T message)
    {
        foreach(IObserver<T> observer in observers)
        {
            observer.Notify(message);
        }
    }
}

public interface IObserver<T>
{
    void Notify(T message);
}

//interface to indicate that a monobehaviour has an observable on it
public interface IObservable<T>
{
    Observable<T> Observable();
    //  { return <*your observable here*>; }
}

//base message; child classes can be used to carry data
public class Message 
{
    public readonly string messageType;
    public Message(string type)
    {
        messageType = type;
    }
}
//I suggest that child classes of message have hard-coded names to make filtering and casting the incoming messages easier (you can use switch statements that way)

//static class that uses strings to route messages. messages must be cast on the recieving end

public static class Observers
{
    static Dictionary<string, Observable<Message>> theObservables = new Dictionary<string, Observable<Message>>();

    public static void Subscribe(IObserver<Message> subscriber, params string[] messageTypes)
    {
        foreach (string messageType in messageTypes)
        {
            if (!theObservables.ContainsKey(messageType))
                theObservables[messageType] = new Observable<Message>();

            theObservables[messageType].Subscribe(subscriber);
        }
    }

    public static void Unsubscribe(IObserver<Message> subscriber, params string[] messageTypes)
    {
        foreach (string messageType in messageTypes)
            if (theObservables.ContainsKey(messageType))
                theObservables[messageType].Unsubscribe(subscriber);
    }

    public static void Post(Message message)
    {
        theObservables[message.messageType].Post(message);
    }
}