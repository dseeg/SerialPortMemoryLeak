# SerialPortMemoryLeak
Demonstrates a possible memory leak(?) with SerialPort in .NET 5 under Linux

### .NET Version


### Occures on
```
Raspberry Pi 3 Model B
OS: Raspbian 9.13 stretch
Kernel: armv71 Linux 4.19.66-v7+
CPU: ARMv7 rev 5 (v71) @ 900MHz
RAM: 1GB
```

### Does not occur on
```
OS: Windows 8.1
CPU: i5-2500k
RAM: 16GB
```

### Untested
Other flavors of Unix
Other versions of Windows

# Background
These tests were run with an Arduino connected to a Raspberry Pi via USB cable.

To ensure a TimeoutException occurs, I used a sketch with an empty loop() to so it would not send any data over serial. SerialPort.ReadTimeout was set to 1.

To compare it with a run where a TimeoutExceptions did not occur, I used a sketch that transmitted data every iteration of loop(). SerialPort.ReadTimeout was set to SerialPort.InfiniteTimeout

# Problem
### Timeout = 1 
When using SerialPort.Read and a TimeoutException occures, the reference for the byte[] used is never released by the underlying SerialStream used by SerialPort.

Monitoring the process with dotnet-counters shows the GC Heap Size continuously growing (data taken when run was complete) and gcroots aren't cleared. Count/total size for byte[] is outrageous. Also notice that SerialStreamIORequest count is equal to the number of iterations (20000):

#### dotnet-counters
```
Press p to pause, r to resume, q to quit.
    Status: Running

[System.Runtime]
    % Time in GC since last GC (%)                                 0
    Allocation Rate (B / 1 sec)                               33,260
    CPU Usage (%)                                                  1
    Exception Count (Count / 1 sec)                                0
    GC Fragmentation (%)                                           7.642
    GC Heap Size (MB)                                             24
    Gen 0 GC Count (Count / 1 sec)                                 1
    Gen 0 Size (B)                                             9,884
    Gen 1 GC Count (Count / 1 sec)                                 0
    Gen 1 Size (B)                                           485,444
    Gen 2 GC Count (Count / 1 sec)                                 0
    Gen 2 Size (B)                                        25,744,976
    IL Bytes Jitted (B)                                       75,380
    LOH Size (B)                                             131,120
    Monitor Lock Contention Count (Count / 1 sec)                  0
    Number of Active Timers                                        0
    Number of Assemblies Loaded                                   19
    Number of Methods Jitted                                     735
    POH (Pinned Object Heap) Size (B)                         20,016
    ThreadPool Completed Work Item Count (Count / 1 sec)           0
    ThreadPool Queue Length                                        0
    ThreadPool Thread Count                                        0
```

#### dumpheap -stat
```
...
6604d32c       11       262276 System.Collections.Concurrent.ConcurrentQueueSegment`1+Slot[[System.IO.Ports.SerialStream+SerialStreamIORequest, System.IO.Ports]][]
6a2c3248    20000       560000 System.IO.Ports.SerialStream+SerialStreamIORequest
6601239c    20000       800000 System.Threading.Tasks.Task`1[[System.Int32, System.Private.CoreLib]]
66014c18    20000       880000 System.Threading.Tasks.Task+ContingentProperties
66012008    20002       880088 System.Threading.CancellationTokenSource
011b9e40    31952      2024484      Free
6a2b700c    20025     20732431 System.Byte[]
Total 135477 objects
```

#### gcroot inspection
```
...
53bf10f4 6a2b700c     1036
53bf157c 6a2b700c     1036
53bf1a04 6a2b700c     1036
53bf1e8c 6a2b700c     1036
53bf2314 6a2b700c     1036
53bf27a8 6a2b700c     1036
53bf2c3c 6a2b700c     1036
53bf30c4 6a2b700c     1036
53bf354c 6a2b700c     1036
53bf39f8 6a2b700c     1036
53bf3eb4 6a2b700c     1036
53bf5708 6a2b700c     1036
53bf6a70 6a2b700c     1036
53bfa4a0 6a2b700c     1036
53bfce58 6a2b700c     1036
53c01688 6a2b700c     1036
53c08bcc 6a2b700c     1036

Statistics:
      MT    Count    TotalSize Class Name
6a2b700c    20025     20732431 System.Byte[]
Total 20025 objects

> gcroot 53bfa4a0
Found 0 unique roots (run 'gcroot -all' to see all roots).
> gcroot 53bf30c4
Thread 75d4:
    7E9016F0 6A215A8C SerialPortMemoryLeak.Program.Main(System.String[]) [C:\Users\Dillon\source\repos\SerialPortMemoryLeak\SerialPortMemoryLeak\Program.cs @ 55]
        r11-44: 7e90171c
            ->  62F08D38 System.IO.Ports.SerialPort
            ->  62F09288 System.IO.Ports.SerialStream
            ->  62F09310 System.Collections.Concurrent.ConcurrentQueue`1[[System.IO.Ports.SerialStream+SerialStreamIORequest, System.IO.Ports]]
            ->  53766868 System.Collections.Concurrent.ConcurrentQueueSegment`1[[System.IO.Ports.SerialStream+SerialStreamIORequest, System.IO.Ports]]
            ->  63F00020 System.Collections.Concurrent.ConcurrentQueueSegment`1+Slot[[System.IO.Ports.SerialStream+SerialStreamIORequest, System.IO.Ports]][]
            ->  53BF3508 System.IO.Ports.SerialStream+SerialStreamIORequest
            ->  53BF30C4 System.Byte[]

Found 1 unique roots (run 'gcroot -all' to see all roots).
> gcroot 53bf1e8c
Thread 75d4:
    7E9016F0 6A215A8C SerialPortMemoryLeak.Program.Main(System.String[]) [C:\Users\Dillon\source\repos\SerialPortMemoryLeak\SerialPortMemoryLeak\Program.cs @ 55]
        r11-44: 7e90171c
            ->  62F08D38 System.IO.Ports.SerialPort
            ->  62F09288 System.IO.Ports.SerialStream
            ->  62F09310 System.Collections.Concurrent.ConcurrentQueue`1[[System.IO.Ports.SerialStream+SerialStreamIORequest, System.IO.Ports]]
            ->  53766868 System.Collections.Concurrent.ConcurrentQueueSegment`1[[System.IO.Ports.SerialStream+SerialStreamIORequest, System.IO.Ports]]
            ->  63F00020 System.Collections.Concurrent.ConcurrentQueueSegment`1+Slot[[System.IO.Ports.SerialStream+SerialStreamIORequest, System.IO.Ports]][]
            ->  53BF22D0 System.IO.Ports.SerialStream+SerialStreamIORequest
            ->  53BF1E8C System.Byte[]

Found 1 unique roots (run 'gcroot -all' to see all roots).
```
### Timeout = SerialPort.InfiniteTimeout
If timeout = SerialPort.Infinite, byte[] references are cleared and garbage collection takes them out. Monitoring the process with dotnet-counters shows GC Heap Size cleared out and most of the byte[] references don't have any gcroots. Count/total size is much more reasonable:
```

```

# Speculation
From the data collected by dotnet-counters and dotnet-dump, and inspecting the code for SerialStream.Unix.cs, the byte[] eventually makes its way into a [SerialStreamIORequest and loaded into a queue](https://github.com/dotnet/runtime/blob/607f98c888b31566c773712c3153236c7cec05d2/src/libraries/System.IO.Ports/src/System/IO/Ports/SerialStream.Unix.cs#L435). SerialStreamIORequests [do not seem to be removed from that queue](https://github.com/dotnet/runtime/blob/607f98c888b31566c773712c3153236c7cec05d2/src/libraries/System.IO.Ports/src/System/IO/Ports/SerialStream.Unix.cs#L789) even though they should be marked as completed when the cancellation token fires.

# Sketch
```
#include <Arduino.h>

void setup()
{
  Serial.begin(9600);
}

void loop()
{
  //Serial.write("data");
}
```
