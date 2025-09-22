<template>
  <n-config-provider :theme="theme">
    <n-notification-provider>
      <n-message-provider>
        <div class="container">
          <CustomExecutor/>
        </div>
        <n-global-style/>
      </n-message-provider>
    </n-notification-provider>
  </n-config-provider>
</template>

<script lang="ts" setup>
import {darkTheme, type GlobalTheme, NConfigProvider, NGlobalStyle, useOsTheme, NNotificationProvider} from 'naive-ui'
import {ref} from "vue";

const osThemeRef = useOsTheme()

//初始化主题
const theme = ref<GlobalTheme | null>(osThemeRef.value === "dark" ? darkTheme : null)

//适配系统主题切换
window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
  theme.value = e.matches ? darkTheme : null
})

</script>

<style>
.container {
  height: 100vh;
  padding: 10px;
}
</style>