// Modern OSC Setup
OscIn oin;
OscMsg msg;
6449 => oin.port;

// Listen for this specific address and a string
oin.addAddress( "/chuck-daemon/cmd, s" ); 

1::second => dur beat;

// The Buffer
string orchestra[100]; 
int orchCount;

////////////// This stuff is just for the test function //////////////
TriOsc osc => ADSR env1 => dac;
TriOsc osc2 => ADSR env2 => dac;

(beat / 2, beat / 2, 0, 1::ms) => env1.set;
(1::ms, beat / 8, 0, 1::ms) => env2.set;

0.2 => osc.gain;
0.1 => osc2.gain;

[0,4,7,12] @=> int major[];
[0,3,7,12] @=> int minor[];
48 => int offset;
int position;
//////////////////////////////////////////////////////////////////////

StringTokenizer st;

while ( true ) {
    
    // Wait for ANY OSC message on port 6449
    oin => now; 
    
    // Process all messages in the queue
    while ( oin.recv(msg) ) { 
        
        // Get the first integer argument
        msg.getString(0) => string cmd; 
        
        <<< "received " + cmd >>>;
                
        st.set(cmd);
        st.next() => string command;
        <<< "command is " + command >>>;
        
        if (command.length() == 0){
            <<< "zero-length command" >>>;
        }        
        else if (command == "add") {
            processAddCommand(st);
        } 
        else if (command == "remove") {
            processRemoveCommand(st);
        } 
        else if (command == "play") {
            processPlayCommand(st);
        } 
        else if (command == "score") {
            processScoreCommand(msg);
        } 
        else if (command == "test"){
            // Do something musical with 'note' here!
            playTwoBars(0, minor);
            playTwoBars(-4, major);
            playTwoBars(-2, major);
            playTwoBars(-5, major);
        }
        else {
            <<< "Unknown command" >>>;
        }
    }
    
}

fun void processAddCommand(StringTokenizer st){
    st.next().trim() => string arg;
    <<< "arg is " + arg >>>;
    if (arg.length() == 0){
        <<< "zero-length arg for add" >>>;
    }
    else {
        <<< "add " + arg >>>;        
        me.dir() + arg => orchestra[orchCount++];
    }
}

fun void processRemoveCommand(StringTokenizer st){
    st.next().trim() => string arg;
    <<< "arg is " + arg >>>;

    <<< "remove " + arg >>>;
    for (0 => int i; i < orchCount; i++) {
        if (orchestra[i] == me.dir() + arg) {
            orchestra[orchCount - 1] => orchestra[i];
            orchCount--;
            "" => orchestra[orchCount];
            <<< "removed " + arg >>>;
            break;
        }
    }
}

fun void processPlayCommand(StringTokenizer st){
    st.next() => string raw;
    Std.atoi(raw) => int arg;
    <<< "arg is " + arg >>>;

    <<< "play " + arg >>>;
    
    int activeIds[orchCount];

    for (0 => int i; i < orchCount; i++) {
        Machine.add(orchestra[i]) => activeIds[i];
    }
    
    beat * arg => now;
    
    for (0 => int i; i < orchCount; i++) {
        Machine.remove(activeIds[i]);
    }
}


fun void processScoreCommand(OscMsg msg){
    // Get the number of commands to process
    msg.getInt(1) => int count; 
    for (2 => int i; i < count + 2; i++) {
        msg.getString(i) => string cmd;
        st.set(cmd);
        if (cmd == "add") {
            // TODO
        }
        else if (cmd == "remove") {
            // TODO
        }
        else if (cmd == "play") {
            // TODO
        }
        
        /*while(true){
            st.next() => string token;
            if (token.length() == 0) {
                break;
            }
        }*/
    }
}

fun void playTwoBars(int position, int chord[])
{
    for(0 => int i; i < 4; i++)
    {
        Std.mtof(chord[0] + offset + position) => osc.freq;
        1 => env1.keyOn;
        for(0 => int j; j < 4; j++)
        {
            Std.mtof(chord[j] + offset + position + 12) => osc2.freq;
            1 => env2.keyOn;
            beat / 8 => now;
        }
    }
}