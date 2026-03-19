import torch

def compute_advantage(rewards, values, gamma=0.99):
    advantages = []
    gae = 0

    for i in reversed(range(len(rewards))):
        delta = rewards[i] + gamma * values[i+1] - values[i]
        gae = delta + gamma * gae
        advantages.insert(0, gae)

    return advantages

def compute_loss(new_log_probs, old_log_probs, advantage, entropy):
    ratio = (new_log_probs - old_log_probs).exp()
    clip = torch.clamp(ratio, 0.8, 1.2)

    loss = -torch.min(
        ratio * advantage,
        clip * advantage
    ).mean()

    return loss - 0.01 * entropy
