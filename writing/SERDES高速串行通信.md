SerDes艺术

**Stauffer, David Robert, et al. *High speed serdes devices and applications*. Springer Science & Business Media, 2008.** 这是一本面向高速串行通信（Serializer/Deserializer，SerDes）领域的经典工程技术书籍，主要介绍 **Gbps 级高速串行链路的芯片设计、系统架构、信号完整性问题以及实际应用**。它出版于 2008 年，正处于 PCI Express、10Gb Ethernet、光纤通信、高速 FPGA 收发器快速发展的阶段，因此内容非常贴近工程实践。

简单来说，这本书回答的问题是：为什么高速接口要从并行变成串行？一个几Gbps甚至几十Gbps的SerDes链路，芯片内部如何实现？高速信号为什么会失真？如何设计、测试和调试？

Ser——Serializer，串行器，串化器，将并行数据转为串行发送；

Des——Deserializer，解串器，将接收到的串行数据转为并行数据处理。

## 1. 并行数据总线的弊端

大位宽下所需I/O数量爆炸；布线占地大、等长时序难满足；虽然位宽大，但是牵一发而动全身，整体速率难以提升；不适合高数据吞吐密度工程应用。

## 2. 源同步接口Source Synchronous Interface

源同步接口架构，这种接口有独立的时钟用于同步接收数据。时钟可以是发送和接收共用的参考时钟，或是由发送端驱动到接收端的时钟。在这种架构中，不需要时钟恢复电路（clock recovery circuits）。

### 减少IO数量

### 时钟转发 Clock Forwarding







