# RabbitMQ.Core  
 The Unofficial Port of the Official RabbitMQ DotNet Client to pure NetCore 3.x  
 
# NOTICE
Moving all my re-usable work to a set of NuGets/Libraries to better manage library development and releases. Sorry for the inconvenience but I just need a little consolidation to help me manage things easier. Proper organization/domain isolation will allow for more effective library writing, testing, re-usability.  
https://github.com/houseofcat/Library
 
# RABBITMQ.CORE DEPRECATED  
Replaced by:
* HouseofCat.RabbitMQ.Client  

# COOKEDRABBIT.CORE DEPRECATED
Replaced by several nuget packages below! Enjoy!
 * HouseofCat.RabbitMQ
 * HouseofCat.RabbitMQ.Services
 * HouseofCat.RabbitMQ.Pipelines
 * HouseofCat.Workflows
 * HouseofCat.Workflows.Pipelines
 * HouseofCat.Compression
 * HouseofCat.Encryption
 * HouseofCat.Extensions
 * HouseofCat.Utilities

Namespace migration will be from `CookedRabbit.Core` to `HouseofCat.XXXX`. Also note, 4.0.0 is now cancelled in favor of HouseofCat.RabbitMQ v1.0.0 as soon as I finish cleaning it up.

## I will be maintaining and hotfixing CookedRabbit.Core 3.3.x though so don't fear!!!

### Old RabbitMQ.Core.Client NetCore 
[![NuGet](https://img.shields.io/nuget/dt/RabbitMQ.Core.Client.svg)](https://www.nuget.org/packages/RabbitMQ.Core.Client/)  
[![NuGet](https://img.shields.io/nuget/v/RabbitMQ.Core.Client.svg)](https://www.nuget.org/packages/RabbitMQ.Core.Client/) 

#### Versions Explained

v1.0.x - Modern development based on v5.1.2 official client (NetCore v2.2-3.1 | NetStandard (2.0-2.1)    
v1.0.60x - Current development based on v6.0.0 official client (NetStandard 2.1 | NetCore v3.1)  
v1.0.610 - Current development based on v6.1.0 official client (NetStandard 2.1 | NetCore v3.1 | Net5.0)  
 
 * RabbitMQ.Core.Client [Readme](https://github.com/houseofcat/RabbitMQ.Core/tree/master/v6.0.0)  

### Old CookedRabbit.Core NetCore
![.NET Core](https://github.com/houseofcat/RabbitMQ.Core/workflows/CookedRabbitBuild/badge.svg?branch=master)  
[![Codacy Badge](https://api.codacy.com/project/badge/Grade/becca6e3d3c0474891007fc83d33a2e3)](https://app.codacy.com/manual/cat_3/RabbitMQ.Core?utm_source=github.com&utm_medium=referral&utm_content=houseofcat/RabbitMQ.Core&utm_campaign=Badge_Grade_Dashboard)  
[![NuGet](https://img.shields.io/nuget/dt/CookedRabbit.Core.svg)](https://www.nuget.org/packages/CookedRabbit.Core/)   
[![NuGet](https://img.shields.io/nuget/v/CookedRabbit.Core.svg)](https://www.nuget.org/packages/CookedRabbit.Core/)  

#### Versions Explained

v1.x.x - Legacy development with only official RabbitMQ.Client - hotfix only.   
v2.0.x - Modern development based on RabbitMQ.Core.Client v5.1.2 - all new work (NetCore 2.2-3.1).   
v2.6.x - Modern development based on RabbitMQ.Core.Client v6.0.0 - all new work (NetCore 3.1).   
v3.0.0-v3.2.1 - Major library overhaul to pipelines and Consumer consolidation and a lot of cleanup.  
v3.3.0+ - Removing legacy/older consumers. NET5.0 support begins.  
v4.x.x - Migrating to HouseofCat.RabbitMQ v1.0.0

 * CookedRabbit.Core [Readme](https://github.com/houseofcat/RabbitMQ.Core/tree/master/CookedRabbit.Core)  

### Prototype Client  
Was a total rewrite conducted by Github user `bording` and the PrototypeClient is based off of their Angora repo.  

It is located here:  
https://github.com/bording/Angora  
