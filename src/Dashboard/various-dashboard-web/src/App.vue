<template>
  <n-config-provider :theme="theme">
    <n-notification-provider>
      <n-modal-provider>
        <n-message-provider>
          <n-dialog-provider>
            <div class="container">
              <n-space vertical :size="20">
                <n-card class="shadow">
                  <n-page-header subtitle="测试版 0.0.1">
                    <n-grid :cols="5">
                      <n-gi>
                        <n-statistic label="数据库数量" value="25"/>
                      </n-gi>
                      <n-gi>
                        <n-statistic label="活跃" value="20"/>
                      </n-gi>
                      <n-gi>
                        <n-statistic label="空闲" value="5"/>
                      </n-gi>
                      <n-gi>
                        <n-statistic label="本地消息表" value="83 个"/>
                      </n-gi>
                      <n-gi>
                        <n-statistic label="多库事务" value="2,346 个"/>
                      </n-gi>
                    </n-grid>
                    <template #title>
                      🌟FreeSql.Various
                    </template>
                    <template #header>
                      <n-breadcrumb>
                        <n-breadcrumb-item>本地消息表日志</n-breadcrumb-item>
                        <n-breadcrumb-item>多库事务日志</n-breadcrumb-item>
                        <n-breadcrumb-item>数据库统计</n-breadcrumb-item>
                      </n-breadcrumb>
                    </template>
                    <template #extra>
                      <n-space>
                        <n-switch v-model:value="active">
                          <template #checked>
                            暗黑模式
                          </template>
                          <template #unchecked>
                            明亮模式
                          </template>
                        </n-switch>
                      </n-space>
                    </template>
                    <template #footer>
                      {{ nowDateTime }}
                    </template>
                  </n-page-header>
                </n-card>
                <n-card class="shadow" title="自定义操作">
                  <CustomExecutor/>
                </n-card>
              </n-space>
            </div>
          </n-dialog-provider>
          <n-global-style/>
        </n-message-provider>
      </n-modal-provider>
    </n-notification-provider>
  </n-config-provider>
</template>

<script lang="ts" setup>
import {darkTheme, type GlobalTheme, NConfigProvider, NGlobalStyle, useOsTheme, NNotificationProvider} from 'naive-ui'

import {ref, watch} from "vue";

const active = ref(false)

const osThemeRef = useOsTheme()

const isDark = ref(osThemeRef.value === "dark")

const nowDateTime = ref(new Date().toLocaleString())

setInterval(() => {
  nowDateTime.value = new Date().toLocaleString()
}, 1000)

//初始化主题
const theme = ref<GlobalTheme | null>(isDark.value ? darkTheme : null)

active.value = isDark.value

//监听切换事件
watch(active, (newValue: any) => {
  theme.value = newValue ? darkTheme : null
})

//适配系统主题切换
window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
  theme.value = e.matches ? darkTheme : null
})

</script>

<style>
.container {
  padding: 20px;
}

.shadow {
  -webkit-box-shadow: 0px 0px 19px 0px rgba(30, 66, 153, 0.1);
  -moz-box-shadow: 0px 0px 19px 0px rgba(30, 66, 153, 0.1);
  box-shadow: 0px 0px 19px 0px rgba(30, 66, 153, 0.1);
}
</style>