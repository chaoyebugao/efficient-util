``` mermaid
graph TD
A[开始] --> B{SSO会话有效?}
B -->|是| C[跳过登录]
B -->|否| D[执行SSO登录]
C & D --> E[获取ECR密码]
E --> F[构建Docker命令]
F --> G[复制到剪贴板]
G --> H[结束]
```