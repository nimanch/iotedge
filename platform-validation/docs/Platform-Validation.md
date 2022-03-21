Platform Compatibility Tool

```mermaid
graph LR
    A[Operator] -->|Run IoT Edge Compatibility Tool|B(aka.ms/aziot-compatibility)
    B --> C{Compatible?}
    C -->D[Run Validation Tests]
    C -->E[Remediate Device]
```