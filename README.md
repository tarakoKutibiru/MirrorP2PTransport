# MirrorP2PTransport

## これはなに？

Unityでリアルタイムネットワーク対戦ゲームをP2Pで作るための仕組みです。

[Mirror](https://github.com/vis2k/Mirror)のTransportの１つです。WebRTCのDataChannelを使ってP2P通信を実現します。
WebRTCのシグナリングサーバには時雨堂様のAyameを利用しています。非常に実験的でまだ検証兼開発を行っている最中です。

## 特徴

- Host Client間の接続にWebRTCのDataChannelを使っている。
- シグナリングサーバには時雨堂様のAyame Laboを使っている。
- 現時点で1対1の通信しかできない。
- Windows/Mac/iOS/Androidに対応

## このプロジェクトの目的

WebRTCのDataChannelを使えば、手軽にNAT超えしたマルチネットワークゲームを作れるのではないか？という疑問の検証のために趣味で開発しています。

WebRTCのDataChannelを使ったライブラリをゼロから自前で全部実装するのは大変疲れるため出来る限り楽をしたいと考えました。UNetの有力な後継であるMirrorの通信部分をWebRTCのDataChannelに置き換えてしまえばMirrorの豊富な資産が使えて幸せなのではないかと考えたわけです。

さらに時雨堂様が無料で公開してくださっているシグナリングサーバAyame Laboを使えば、自分でサーバを立てる必要もないのではないか？と考えました。


## このプロジェクトの目標

- 完全に無料でインターネット越しにP2Pでリアルタイム通信を実現したい。
- 1対1だけじゃなくて４人対戦とかできるようにしたい。

## 使い方

外部リンクになりますが、こちらに使い方と簡単な解説を書いています。

https://zenn.dev/5ena/articles/184f208f7a1d03e1d876
