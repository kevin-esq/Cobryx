import torch
import torch.nn as nn
import torch.distributions as dist

class ActorCritic(nn.Module):
    def __init__(self):
        super().__init__()
        self.shared = nn.Sequential(
            nn.Linear(5, 64),
            nn.ReLU(),
            nn.Linear(64, 32),
            nn.ReLU()
        )
        self.actor = nn.Linear(32, 2)
        self.log_std = nn.Parameter(torch.zeros(2))
        self.critic = nn.Linear(32, 1)

    def forward(self, x):
        base = self.shared(x)
        return self.actor(base), self.critic(base)

def sample_action(mean, log_std):
    std = torch.exp(log_std)
    d = dist.Normal(mean, std)
    
    raw_action = d.sample()
    log_prob = d.log_prob(raw_action).sum(dim=-1, keepdim=True)
    
    action = torch.tanh(raw_action)
    return raw_action, action, log_prob
